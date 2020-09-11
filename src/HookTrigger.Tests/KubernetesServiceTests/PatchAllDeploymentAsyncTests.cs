using FluentAssertions;
using HookTrigger.Worker.Brokers;
using HookTrigger.Worker.Services;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HookTrigger.Tests.KubernetesServiceTests
{
    public class PatchAllDeploymentAsyncTests
    {
        private readonly Mock<IKubernetesBroker> _broker;
        private readonly IKubernetesService _kubernetesService;

        public PatchAllDeploymentAsyncTests()
        {
            _broker = new Mock<IKubernetesBroker>();
            _kubernetesService = new KubernetesService(new Mock<ILogger<KubernetesService>>().Object, _broker.Object);
        }

        [Theory]
        [InlineData("nginx", "latest", "test")]
        public async Task ShouldPatchDeploymentsAsync(string imageName, string tag, string protectedNamespace)
        {
            var deployList = GetDeployList(protectedNamespace, 2);

            _broker.Setup(x => x.FindDeploymentByImageAsync(imageName)).ReturnsAsync(deployList);

            var patchedDeploys = await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            patchedDeploys.Should().Be(2);
        }

        [Theory]
        [InlineData("nginx", "latest", "kube-system")]
        public async Task ShouldThrowIfDeploymentsAreInProtectedNamespacesAsync(string imageName, string tag, string protectedNamespace)
        {
            var deployList = new List<V1Deployment> {
            new V1Deployment
            {
                 Metadata = new V1ObjectMeta
                 {
                     NamespaceProperty = protectedNamespace
                 }
            }
            };

            _broker.Setup(x => x.FindDeploymentByImageAsync(imageName)).ReturnsAsync(deployList);

            async Task patch() => await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            await Assert.ThrowsAsync<InvalidOperationException>(patch);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("image", "")]
        [InlineData("", "tag")]
        [InlineData(" ", "tag")]
        [InlineData("image", " ")]
        [InlineData(" ", " ")]
        [InlineData(null, null)]
        public async Task ShouldThrowIfImageAndTagAreNullOrWhiteSpaceAsync(string imageName, string tag)
        {
            async Task patch() => await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            await Assert.ThrowsAsync<ArgumentException>(patch);
        }

        private static List<V1Deployment> GetDeployList(string protectedNamespace, int numberOfDeploys)
        {
            var output = new List<V1Deployment>();

            for (var i = 0; i < numberOfDeploys; i++)
            {
                output.Add(new V1Deployment
                {
                    Metadata = new V1ObjectMeta
                    {
                        NamespaceProperty = protectedNamespace
                    },
                    Spec = new V1DeploymentSpec
                    {
                        Template = new V1PodTemplateSpec
                        {
                            Spec = new V1PodSpec
                            {
                                Containers = new List<V1Container>
                                 {
                                     new V1Container
                                     {
                                         Image = "nginx"
                                     },
                                     new V1Container
                                     {
                                        Image = "nginx"
                                     },
                                    new V1Container
                                    {
                                            Image = "differentImage"
                                    }
                                 }
                            }
                        }
                    }
                });
            }

            return output;
        }
    }
}