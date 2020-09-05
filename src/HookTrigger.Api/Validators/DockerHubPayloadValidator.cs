using FluentValidation;
using HookTrigger.Core.Models;

namespace HookTrigger.Api.Validators
{
    public class DockerHubPayloadValidator : AbstractValidator<DockerHubPayload>
    {
        public DockerHubPayloadValidator()
        {
            RuleFor(p => p.PushData).Cascade(CascadeMode.Stop).NotNull();
            RuleFor(p => p.PushData.Tag).Cascade(CascadeMode.Stop).NotEmpty().Length(2, 128);
            RuleFor(p => p.Repository).Cascade(CascadeMode.Stop).NotNull();
            RuleFor(p => p.Repository.RepoName).Cascade(CascadeMode.Stop).NotEmpty().Length(2, 255);
        }
    }
}