using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Tokens.Validators;
public class GenerateTokenValidator : AbstractValidator<TokenGenerationDto>
{
    public GenerateTokenValidator()
    {
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress();

        RuleFor(p => p.Password).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(p => p.RememberMe).NotNull();
    }
}
