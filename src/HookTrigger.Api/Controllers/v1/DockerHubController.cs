using GenFu;
using HookTrigger.Api.Services;
using HookTrigger.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IMessageSenderService<DockerHubPayload> _messageSender;

        #region Constructor

        public DockerHubController(ILogger<DockerHubController> logger, IMessageSenderService<DockerHubPayload> messageSender)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        }

        #endregion Constructor

        #region Methods

        #region HttpPost

        [HttpPost()]
        [Produces("application/json")]
        [ProducesResponseType(typeof(DockerHubPayload), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDockerHubPayloadAsync([FromBody] DockerHubPayload payload, ApiVersion version)
        {
            await _messageSender.SendMessageAsync(payload);

            return Ok();
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