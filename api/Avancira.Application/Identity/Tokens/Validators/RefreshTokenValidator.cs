using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Tokens.Validators;
public class RefreshTokenValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
    }
}