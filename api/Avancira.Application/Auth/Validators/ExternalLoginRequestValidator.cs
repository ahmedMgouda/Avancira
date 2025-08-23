using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using FluentValidation;

namespace Avancira.Application.Auth.Validators;

public class ExternalLoginRequestValidator : AbstractValidator<ExternalLoginRequest>
{
    public ExternalLoginRequestValidator(IExternalAuthService authService)
    {
        RuleFor(x => x.Provider)
            .Must(p => authService.SupportsProvider(p))
            .WithMessage("Unsupported provider");

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}

