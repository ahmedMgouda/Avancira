using Avancira.Application.Identity.Tokens.Dtos;
using FluentValidation;

namespace Avancira.Application.Identity.Tokens.Validators;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenValidator()
    {
        // Access token is optional when refreshing, so no validation rules are required.
    }
}
