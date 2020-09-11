using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker.Services
{
    public interface IKubernetesService
    {
        Task<int> PatchAllDeploymentAsync(string repoName, string tag, CancellationToken cancellationToken = default);
    }
}