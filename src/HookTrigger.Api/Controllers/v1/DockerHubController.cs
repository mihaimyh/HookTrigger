using HookTrigger.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HookTrigger.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:ApiVersion}/[controller]")]
    public class DockerHubController : ControllerBase
    {
        private readonly ILogger<DockerHubController> _logger;

        public DockerHubController(ILogger<DockerHubController> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        [HttpPost()]
        [Produces("application/json", "application/xml")]
        [ProducesResponseType(typeof(DockerHubPayload), StatusCodes.Status201Created)]
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