using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Services
{
    public interface IKubernetesService
    {
        public Task PatchAllDeploymentAsync(string repoName, string tag, CancellationToken cancellationToken = default);
    }
}