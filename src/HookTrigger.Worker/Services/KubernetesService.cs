using k8s;
using k8s.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Services
{
    internal class KubernetesService : IKubernetesService
    {
        private readonly ILogger<KubernetesService> _logger;
        private IKubernetes _client;

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

        public async Task RestartDeploymentAsync(string repoName, string tag)
        {
            try
            {
                //await CreateDeploymentAsync();

                var deployment = await DeleteDeploymentAsync(repoName);

                if (deployment != null)
                {
                    _logger.LogDebug("Creating a new deployment with name {Name} in namespace {Namespace}.", deployment?.Metadata?.Name, deployment?.Metadata?.NamespaceProperty);

                    deployment.Metadata.ResourceVersion = string.Empty;

                    var deploy = await _client.CreateNamespacedDeploymentAsync(deployment, deployment?.Metadata?.NamespaceProperty);

                    _logger.LogDebug("A new deployment with id {Id} was created at {Timestamp}.", deploy?.Metadata?.Uid, deploy?.Metadata?.CreationTimestamp);
                }
            }
            catch (HttpOperationException ex)
            {
                _logger.LogError(ex, ex.Response.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to restart deployment.");
            }
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

        private async Task<V1Deployment> DeleteDeploymentAsync(string repoName)
        {
            var deployment = await FindDeploymentByImageAsync(repoName);

            if (deployment != null)
            {
                _logger.LogDebug("Deleting deployment with name {Name}.", deployment?.Metadata?.Name);

                var status = await _client.DeleteNamespacedDeploymentAsync(deployment?.Metadata?.Name, deployment?.Metadata?.NamespaceProperty);

                _logger.LogDebug(status.Status);

                //await _client.ReadNamespacedDeploymentAsync(nginxDeploy.Metadata.Name, "default");
            }

            return deployment;
        }

        private async Task<V1Deployment> FindDeploymentByImageAsync(string imageName)
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

            var deployment = deployments?.Items?.ToList().Find(x => x.Spec.Template.Spec.Containers[0].Image.Contains(imageName));

            if (deployment is null)
            {
                _logger.LogDebug("No deployments having container image {image} were found.", imageName);
                return null;
            }

            _logger.LogDebug("Found deployment {name} under namespace {namespace}.", deployment?.Metadata?.Name, deployment?.Metadata?.NamespaceProperty);

            return deployment;
        }
    }
}