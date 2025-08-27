using FluentValidation;

namespace Avancira.Infrastructure.Auth;

public class TokenRequestParamsValidator : AbstractValidator<TokenRequestParams>
{
    public TokenRequestParamsValidator()
    {
        RuleFor(x => x.GrantType).NotEmpty();

        When(x => x.GrantType == AuthConstants.GrantTypes.AuthorizationCode, () =>
        {
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.RedirectUri).NotEmpty();
            RuleFor(x => x.CodeVerifier).NotEmpty();
        });

        When(x => x.GrantType == AuthConstants.GrantTypes.UserId, () =>
        {
            RuleFor(x => x.UserId).NotEmpty();
        });

        When(x => x.GrantType == AuthConstants.GrantTypes.RefreshToken, () =>
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        });
    }
}
