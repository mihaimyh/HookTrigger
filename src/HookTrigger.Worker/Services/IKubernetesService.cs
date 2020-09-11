using System.Threading.Tasks;

namespace HookTrigger.Worker.Services
{
    public interface IKubernetesService
    {
        public Task RestartDeploymentAsync(string repoName, string tag);
    }
}