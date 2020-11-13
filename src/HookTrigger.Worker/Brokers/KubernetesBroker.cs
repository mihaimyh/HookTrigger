using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Brokers
{
    public class KubernetesBroker : IKubernetesBroker
    {
        private readonly ILogger<KubernetesBroker> _logger;
        private IKubernetes _client;

        public KubernetesBroker(ILogger<KubernetesBroker> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

            ConfigureK8sClient();
        }

        public async Task<List<V1Deployment>> FindDeploymentsByImageAsync(string imageName)
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

        public async Task<V1Deployment> PatchNamespacedDeploymentAsync(V1Patch patch, string deploymentName, string namespaceProperty,
                    CancellationToken cancellationToken = default)
        {
            return await _client.PatchNamespacedDeploymentAsync(patch, deploymentName, namespaceProperty, cancellationToken: cancellationToken);
        }

        private void ConfigureK8sClient()
        {
            var k8SClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
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
            var deployments = await FindDeploymentsByImageAsync(repoName);

            if (deployments?.Count > 0)
            {
                deployments.ForEach(x => _logger.LogDebug("Deleting deployment with name {Name} from namespace {Namespace}.", x?.Metadata?.Name, x?.Metadata.NamespaceProperty));
                deployments.ForEach(async x => await _client.DeleteNamespacedDeploymentAsync(x?.Metadata?.Name, x?.Metadata?.NamespaceProperty));
            }

            return deployments;
        }

        private List<V1Deployment> GetDeploymentsByImageName(string imageName, V1DeploymentList deployments)
        {
            var matchingDeployments = deployments?.Items?.ToList().FindAll(x =>
            {
                foreach (var container in x.Spec?.Template?.Spec?.Containers)
                {
                    if (container.Image.ToLowerInvariant().Contains(imageName.ToLowerInvariant()))
                    {
                        _logger.LogDebug("Found deployment with name {Name} in namespace {Namespace}", x?.Metadata?.Name, x?.Metadata?.NamespaceProperty);

                        return true;
                    }
                }

                return false;
            });
            return matchingDeployments;
        }
    }
}
