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

        public async Task<int> PatchAllDeploymentAsync(string imageName, string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                throw new ArgumentException($"'{nameof(imageName)}' cannot be null or whitespace", nameof(imageName));
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException($"'{nameof(tag)}' cannot be null or whitespace", nameof(tag));
            }

            try
            {
                var deployments = await _kubernetesBroker.FindDeploymentsByImageAsync(imageName);

                ThrowIfProtectedNamespace(_reservedNamespaces, deployments);

                var updatedDeployments = UpdateImageTag(imageName, tag, deployments);

                //TODO: Return a more detailed response?

                return await PatchDeploymentsAsync(updatedDeployments, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to patch the deployment.");
                throw;
            }
        }

        private JsonPatchDocument<V1Deployment> CreateJsonPatchDocument(V1Deployment deployment)
        {
            var patch = new JsonPatchDocument<V1Deployment>();
            var random = new Random();

            patch.Replace(s => s.Spec.Template.Spec, deployment.Spec.Template.Spec);
            patch.Replace(s => s.Spec.Template.Spec.TerminationGracePeriodSeconds, random.Next(10, 30));

            return patch;
        }

        private async Task<int> PatchDeploymentsAsync(List<V1Deployment> deployments, CancellationToken cancellationToken = default)
        {
            var patchTasks = new List<Task>();

            if (deployments?.Count > 0)
            {
                foreach (var deployment in deployments)
                {
                    var jsonPatch = CreateJsonPatchDocument(deployment);

                    _logger.LogDebug("Deployment {@Deployment} marked for patching.", deployment);

                    patchTasks.Add(_kubernetesBroker.PatchNamespacedDeploymentAsync(new V1Patch(jsonPatch),
                                                                                    deployment.Metadata?.Name,
                                                                                    deployment?.Metadata?.NamespaceProperty,
                                                                                    cancellationToken));
                }
            }

            _logger.LogDebug("Sending patch requests to K8S API server.");
            await Task.WhenAll(patchTasks);

            var successfullTasks = patchTasks.FindAll(x => x.IsCompletedSuccessfully).Count;

            var failedTasks = patchTasks.FindAll(x => x.IsFaulted);

            _logger.LogDebug("Successfully patched {Number} deployment(s).", successfullTasks);

            if (failedTasks?.Count > 0)
            {
                _logger.LogError("Following tasks failed, @{FailedTasks}", failedTasks);
            }

            return successfullTasks;
        }

        private void SetImageTag(string tag, V1Container container)
        {
            var image = container?.Image?.Split(":")[0];
            _logger.LogDebug("Setting image tag to {Tag}.", tag);
            if (!string.IsNullOrWhiteSpace(image))
            {
                image = $"{image}:{tag}";
                _logger.LogDebug("New image name is {Image}", image);
                container.Image = image;
            }
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

        private List<V1Deployment> UpdateImageTag(string imageName, string tag, List<V1Deployment> deployments)
        {
            var updatedDeployments = new List<V1Deployment>();

            if (deployments?.Count > 0)
            {
                foreach (var deployment in deployments)
                {
                    foreach (var container in deployment?.Spec?.Template?.Spec?.Containers.SkipWhile(x => !x.Image.ToLowerInvariant()
                                                                                                            .Equals($"{imageName.ToLowerInvariant()}:" +
                                                                                                            $"{tag.ToLowerInvariant()}")))
                    {
                        if (container is null)
                        {
                            // Log it and go to the next container.
                            _logger.LogDebug("Deployment {Deployment} has a null container, skipping it.", deployment?.Metadata?.Name);
                            continue;
                        }
                        SetImageTag(tag, container);

                        if (!updatedDeployments.Contains(deployment))
                        {
                            updatedDeployments.Add(deployment);
                        }
                    }
                }
            }

            return updatedDeployments;
        }
    }
}