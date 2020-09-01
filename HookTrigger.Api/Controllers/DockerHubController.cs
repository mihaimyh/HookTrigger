using HookTrigger.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HookTrigger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DockerHubController : ControllerBase
    {
        private readonly ILogger<DockerHubController> _logger;

        public DockerHubController(ILogger<DockerHubController> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        [HttpPost()]
        public Task<IActionResult> CreateDockerHubPayloadAsync([FromBody] DockerHubPayload payload)
        {
            if (payload is null)
            {
                _logger.LogError($"A null {nameof(DockerHubPayload)} detected.");
                return Task.FromResult<IActionResult>(BadRequest("An invalid payload was detected."));
            }

            return Task.FromResult<IActionResult>(Ok());
        }
    }
}