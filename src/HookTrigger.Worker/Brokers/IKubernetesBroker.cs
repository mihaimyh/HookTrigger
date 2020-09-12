using k8s.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Brokers
{
    public interface IKubernetesBroker
    {
        Task<List<V1Deployment>> FindDeploymentsByImageAsync(string imageName);

        Task<V1Deployment> PatchNamespacedDeploymentAsync(V1Patch patch, string deploymentName, string namespaceProperty, CancellationToken cancellationToken = default);
    }
}