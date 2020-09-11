using k8s;
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
    internal class KubernetesService : IKubernetesService
    {
        private readonly ILogger<KubernetesService> _logger;

        private readonly List<string> _reservedNamespaces = new List<string>{
                    "cert-manager", "ingress-nginx", "kube-system", "kubernetes-dashboard","metallb-system"
         };

        private IKubernetes _client;

        //TODO: Move this to appsettings.json
        public KubernetesService(ILogger<KubernetesService> logger)
        {
            _logger = logger;

            ConfigureClient();
        }

        public async Task ListNamespacesAsync()
        {
            var list = await _client?.ListNamespaceAsync();

            if (list?.Items?.Count > 0)
            {
                foreach (var item in list?.Items)
                {
                    _logger.LogDebug(item.Metadata.Name);
                }
            }
            else
            {
                _logger.LogDebug("No namespaces were found in the cluster.");
            }
        }

        public async Task PatchAllDeploymentAsync(string repoName, string tag, CancellationToken cancellationToken = default)
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
                var deployments = await FindDeploymentByImageAsync(repoName);

                ThrowIfReseverdNamespace(_reservedNamespaces, deployments);

                if (deployments.Count > 0)
                {
                    var containers = deployments[0]?.Spec?.Template?.Spec?.Containers;

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

                        var patchedDeployment = await _client.PatchNamespacedDeploymentAsync(new V1Patch(patch), deployment?.Metadata?.Name,
                             deployment?.Metadata?.NamespaceProperty, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to patch the deployment.");
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

        private void ConfigureClient()
        {
            var k8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
            //var k8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
            _client = new Kubernetes(k8SClientConfig);
        }

        private async Task CreateDeploymentAsync()
        {
            var labels = new Dictionary<string, string> { { "app", "nginx" } };

            var deployment = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = $"nginx-{DateTime.Now.Day}.{DateTime.Now.Hour}.{DateTime.Now.Minute}.{DateTime.Now.Second}.{DateTime.Now.Millisecond}",
                    Labels = labels
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = labels
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = labels
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Image = "nginx",
                                    Name = "nginx",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = 80
                                        }
                                    },
                                    ImagePullPolicy = "Always"
                                }
                            }
                        }
                    }
                },
            };

            _logger.LogDebug("Launching a new Nginx deployment.");

            var deploy = await _client.CreateNamespacedDeploymentAsync(deployment, "default");
        }

        private async Task<List<V1Deployment>> DeleteDeploymentAsync(string repoName)
        {
            var deployments = await FindDeploymentByImageAsync(repoName);

            if (deployments?.Count > 0)
            {
                ThrowIfReseverdNamespace(_reservedNamespaces, deployments);

                deployments.ForEach(x => _logger.LogDebug("Deleting deployment with name {Name} from namespace {Namespace}.", x?.Metadata?.Name, x?.Metadata.NamespaceProperty));
                deployments.ForEach(async x => await _client.DeleteNamespacedDeploymentAsync(x?.Metadata?.Name, x?.Metadata?.NamespaceProperty));
            }

            return deployments;
        }

        private async Task<List<V1Deployment>> FindDeploymentByImageAsync(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                throw new ArgumentException($"'{nameof(imageName)}' cannot be null or whitespace", nameof(imageName));
            }

            var deployments = await _client.ListDeploymentForAllNamespacesAsync();

            if (deployments?.Items?.Count <= 0)
            {
                _logger.LogWarning("No deployments were found on the cluster.");

                return null;
            }

            return GetDeploymentsByImageName(imageName, deployments);

            //if (matchingDeployments?.Count <= 0)
            //{
            //    _logger.LogWarning("No deployments with name matching {Name} were found.", imageName);

            //    var deploymentsContainingName = deployments?.Items?.ToList().FindAll(x => x.Spec.Template.Spec.Containers[0].Image.ToLowerInvariant().Contains(imageName.ToLowerInvariant()));

            //    if (deploymentsContainingName?.Count > 0)
            //    {
            //        deploymentsContainingName?.ForEach(x => _logger.LogDebug("Found following deployment with {Name} in namespace {Namespace}.", x?.Metadata?.Name, x?.Metadata?.NamespaceProperty));
            //    }
            //}
        }

        private List<V1Deployment> GetDeploymentsByImageName(string imageName, V1DeploymentList deployments)
        {
            var matchingDeployments = deployments?.Items?.ToList().FindAll(x =>
            {
                foreach (var container in x.Spec?.Template?.Spec?.Containers)
                {
                    _logger.LogDebug("Found deployment with name {Name} in namespace {Namespace}", x?.Metadata?.Name, x?.Metadata?.NamespaceProperty);
                    return container.Image.Split(":")[0].ToLowerInvariant().Equals(imageName.ToLowerInvariant());
                }

                return false;
                //return x.Spec.Template.Spec.Containers[0].Image.Split(":")[0].ToLowerInvariant().Equals(imageName.ToLowerInvariant());
            });
            return matchingDeployments;
        }

        private void ThrowIfReseverdNamespace(List<string> ignoredNamespaces, List<V1Deployment> deployments)
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