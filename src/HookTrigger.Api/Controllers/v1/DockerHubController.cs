using GenFu;
using HookTrigger.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HookTrigger.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:ApiVersion}/[controller]")]
    public class DockerHubController : ControllerBase
    {
        private readonly ILogger<DockerHubController> _logger;

        #region Constructor

        public DockerHubController(ILogger<DockerHubController> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        #endregion Constructor

        #region Methods

        #region HttpPost

        [HttpPost()]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DockerHubPayload), StatusCodes.Status201Created)]
        public Task<IActionResult> CreateDockerHubPayloadAsync([FromBody] DockerHubPayload payload, ApiVersion version)
        {
            return Task.FromResult<IActionResult>(Ok(payload));

            //return Task.FromResult<IActionResult>(CreatedAtRoute(nameof(GetDockerTriggersAsync), new { value = "string", version }, payload));
        }

        #endregion HttpPost

        #region HttpGet

        [HttpGet(Name = nameof(GetAllDockerTriggersAsync))]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DockerHubPayload), 200)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<IEnumerable<DockerHubPayload>>> GetAllDockerTriggersAsync()
        {
            A.Configure<PushData>();
            A.Configure<Repository>();
            return Task.FromResult<ActionResult<IEnumerable<DockerHubPayload>>>(Ok(A.ListOf<DockerHubPayload>(10)));
        }

        #endregion HttpGet

        #endregion Methods
    }
}