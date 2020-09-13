using FakeItEasy;
using FluentAssertions;
using HookTrigger.Worker.Brokers;
using HookTrigger.Worker.Services;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [InlineData("nginx", "latest", "test", 1)]
        [InlineData("nginx", "latest", "test", 4)]
        [InlineData("nginx", "latest", "test", 0)]
        public async Task ShouldNotPatchDeploymentsWithDifferentContainerImageAsync(string imageName, string tag, string protectedNamespace, int numberOfDeployments)
        {
            var deployList = GetDeployListHavingDifferentContainerImage(imageName, tag, protectedNamespace, numberOfDeployments);

            _broker.Setup(x => x.FindDeploymentsByImageAsync(imageName)).ReturnsAsync(deployList);

            var patchedDeploys = await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            patchedDeploys.Should().Be(0);
        }

        [Theory]
        [InlineData("nginx", "latest", "test", 1)]
        [InlineData("nginx", "latest", "test", 4)]
        [InlineData("nginx", "latest", "test", 0)]
        public async Task ShouldPatchDeploymentsAsync(string imageName, string tag, string protectedNamespace, int numberOfDeployments)
        {
            var deployList = GetDeployListHavingSameContainerImage(imageName, tag, protectedNamespace, numberOfDeployments);

            _broker.Setup(x => x.FindDeploymentsByImageAsync(imageName)).ReturnsAsync(deployList);

            var patchedDeploys = await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            patchedDeploys.Should().Be(numberOfDeployments);
        }

        [Theory]
        [InlineData("nginx", "latest", "test", 1)]
        [InlineData("nginx", "latest", "test", 3)]
        [InlineData("nginx", "latest", "test", 0)]
        public async Task ShouldPatchOnlyMatchingDeploymentsAsync(string imageName, string tag, string protectedNamespace, int numberOfDeployments)
        {
            var deployList = GetDeploysWithExistingAndNonExistingImages(imageName, tag, protectedNamespace, numberOfDeployments);

            _broker.Setup(x => x.FindDeploymentsByImageAsync(imageName)).ReturnsAsync(deployList);

            var patchedDeploys = await _kubernetesService.PatchAllDeploymentAsync(imageName, tag);

            patchedDeploys.Should().Be(numberOfDeployments);
        }

        [Theory]
        [InlineData("nginx", "latest", "kube-system")]
        public async Task ShouldThrowIfDeploymentsAreInProtectedNamespacesAsync(string imageName, string tag, string @namespace)
        {
            var deployList = new List<V1Deployment> {
            new V1Deployment
            {
                 Metadata = new V1ObjectMeta
                 {
                     NamespaceProperty = @namespace
                 }
            }
            };

            _broker.Setup(x => x.FindDeploymentsByImageAsync(imageName)).ReturnsAsync(deployList);

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

        private static List<V1Deployment> GetDeployListHavingDifferentContainerImage(string imageName, string tag, string @namespace, int numberOfDeploys)
        {
            var output = A.CollectionOfFake<V1Deployment>(numberOfDeploys, a => a.ConfigureFake(b =>
            {
                b.Metadata = A.Fake<V1ObjectMeta>(aa => aa.ConfigureFake(bb => bb.NamespaceProperty = @namespace));
                b.Spec = A.Fake<V1DeploymentSpec>(c => c.ConfigureFake(d =>
                {
                    d.Template = A.Fake<V1PodTemplateSpec>(e =>
                    e.ConfigureFake(f =>
                        f.Spec = A.Fake<V1PodSpec>(g =>
                            g.ConfigureFake(h =>
                                h.Containers = A.CollectionOfFake<V1Container>(numberOfDeploys, i =>
                                    i.ConfigureFake(h =>
                                        h.Image = $"{RandomString(numberOfDeploys)}:{tag}"))))));
                }));
            }));

            return output.ToList();
        }

        private static List<V1Deployment> GetDeployListHavingSameContainerImage(string imageName, string tag, string @namespace, int numberOfDeploys)
        {
            var output = A.CollectionOfFake<V1Deployment>(numberOfDeploys, a => a.ConfigureFake(b =>
            {
                b.Metadata = A.Fake<V1ObjectMeta>(aa => aa.ConfigureFake(bb => bb.NamespaceProperty = @namespace));
                b.Spec = A.Fake<V1DeploymentSpec>(c => c.ConfigureFake(d =>
                {
                    d.Template = A.Fake<V1PodTemplateSpec>(e =>
                    e.ConfigureFake(f =>
                        f.Spec = A.Fake<V1PodSpec>(g =>
                            g.ConfigureFake(h =>
                                h.Containers = A.CollectionOfFake<V1Container>(numberOfDeploys, i =>
                                    i.ConfigureFake(h =>
                                        h.Image = $"{imageName}:{tag}"))))));
                }));
            }));

            return output.ToList();
        }

        private static List<V1Deployment> GetDeploysWithExistingAndNonExistingImages(string imageName, string tag, string @namespace, int numberOfDeploys)
        {
            var output = A.CollectionOfFake<V1Deployment>(numberOfDeploys, a => a.ConfigureFake(b =>
            {
                b.Metadata = A.Fake<V1ObjectMeta>(aa => aa.ConfigureFake(bb => bb.NamespaceProperty = @namespace));
                b.Spec = A.Fake<V1DeploymentSpec>(c => c.ConfigureFake(d =>
                {
                    d.Template = A.Fake<V1PodTemplateSpec>(e =>
                    e.ConfigureFake(f =>
                        f.Spec = A.Fake<V1PodSpec>(g =>
                            g.ConfigureFake(h =>
                                h.Containers = new List<V1Container>
                                {
                                    new V1Container
                                    {
                                        Image = $"{imageName}:{tag}"
                                    },
                                    new V1Container
                                    {
                                        Image = $"{RandomString(5)}:{tag}"
                                    }
                                }))));
                }));
            }));

            return output.ToList();
        }

        private static string RandomString(int length)
        {
            var _random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}