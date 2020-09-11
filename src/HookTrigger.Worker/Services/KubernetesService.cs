using HookTrigger.Worker.Brokers;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Services
{
    public class KubernetesService : IKubernetesService
    {
        private readonly IKubernetesBroker _kubernetesBroker;
        private readonly ILogger<KubernetesService> _logger;

        //TODO: Move this to appsettings.json
        private readonly List<string> _reservedNamespaces = new List<string>{
                    "cert-manager", "ingress-nginx", "kube-system", "kubernetes-dashboard","metallb-system"
         };

        public KubernetesService(ILogger<KubernetesService> logger, IKubernetesBroker kubernetesBroker)
        {
            _logger = logger;
            _kubernetesBroker = kubernetesBroker ?? throw new ArgumentNullException(nameof(kubernetesBroker));
        }

        public async Task<int> PatchAllDeploymentAsync(string repoName, string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentException($"'{nameof(repoName)}' cannot be null or whitespace", nameof(repoName));
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException($"'{nameof(tag)}' cannot be null or whitespace", nameof(tag));
            }

            try
            {
                var deployments = await _kubernetesBroker.FindDeploymentByImageAsync(repoName);

                ThrowIfProtectedNamespace(_reservedNamespaces, deployments);

                var patchedDeployments = new List<V1Deployment>();

                if (deployments?.Count > 0)
                {
                    foreach (var deployment in deployments)
                    {
                        var patch = new JsonPatchDocument<V1Deployment>();
                        foreach (var container in (deployment?.Spec?.Template?.Spec?.Containers).Where(container => !string.IsNullOrWhiteSpace(tag)))
                        {
                            _logger.LogDebug("Setting image tag to {Tag}.", tag);
                            var image = container?.Image?.Split(":")[0];
                            if (!string.IsNullOrWhiteSpace(image))
                            {
                                image = $"{image}:{tag}";
                                _logger.LogDebug("New image name is {Image}", image);
                                patch.Replace(e => e.Spec.Template.Spec.Containers[0].Image, image);

                                var random = new Random();
                                patch.Replace(e => e.Spec.Template.Spec.TerminationGracePeriodSeconds, random.Next(10, 30));
                            }
                        }

                        _logger.LogDebug("Sending request to the API to patch the deployment {Deployment}.", deployment?.Metadata?.Name);

                        var patchedDeployment = await _kubernetesBroker.PatchNamespacedDeploymentAsync(new V1Patch(patch), deployment?.Metadata?.Name,
                             deployment?.Metadata?.NamespaceProperty, cancellationToken: cancellationToken);

                        patchedDeployments.Add(patchedDeployment);
                    }
                }

                return patchedDeployments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to patch the deployment.");
                throw;
            }

            //try
            //{
            //    var deployments = await DeleteDeploymentAsync(repoName);

            //    if (deployments?.Count > 0)
            //    {
            //        var deployment = deployments?[0];

            //        _logger.LogDebug("Creating a new deployment with name {Name} in namespace {Namespace}.", deployment?.Metadata?.Name, deployment?.Metadata?.NamespaceProperty);

            //        deployment.Metadata.ResourceVersion = string.Empty;

            //        if (!string.IsNullOrWhiteSpace(tag))
            //        {
            //            _logger.LogDebug("Setting image tag to {Tag}.", tag);

            //            var image = deployment?.Spec?.Template?.Spec?.Containers?[0]?.Image?.Split(":")[0];

            //            if (!string.IsNullOrWhiteSpace(image))
            //            {
            //                image = $"{image}:{tag}";
            //                _logger.LogDebug("New image name is {Image}", image);
            //                deployment.Spec.Template.Spec.Containers[0].Image = image;
            //            }
            //        }

            //        var deploy = await _client.CreateNamespacedDeploymentAsync(deployment, deployment?.Metadata?.NamespaceProperty);

            //        _logger.LogDebug("A new deployment with id {Id} and image {Image} was created at {Timestamp}.", deploy?.Metadata?.Uid, deploy?.Spec?.Template?.Spec?.Containers?[0]?.Image, deploy?.Metadata?.CreationTimestamp);
            //    }
            //    else
            //    {
            //        _logger.LogDebug("No deployments were found.");
            //    }
            //}
            //catch (HttpOperationException ex)
            //{
            //    _logger.LogError(ex, ex.Response.Content);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "An error occurred while trying to restart deployment.");
            //}
        }

        private void ThrowIfProtectedNamespace(List<string> ignoredNamespaces, List<V1Deployment> deployments)
        {
            foreach (var deploy in deployments.Where(deploy => ignoredNamespaces.Contains(deploy?.Metadata?.NamespaceProperty?.ToLowerInvariant())))
            {
                _logger.LogWarning("An attempt to delete the system deployment {Deployment} from {Namespace} namespace was performed.",
                    deploy?.Metadata?.Name,
                    deploy?.Metadata?.NamespaceProperty);
                throw new InvalidOperationException("Cannot delete a deployment from a reserved namespace.");
            }
        }
    }
}