using System.Threading.Tasks;
using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using Avancira.Application.Auth.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class ExternalLoginRequestValidatorTests
{
    private class StubAuthService : IExternalAuthService
    {
        private readonly bool _supports;
        public StubAuthService(bool supports) => _supports = supports;
        public Task<ExternalAuthResult> ValidateTokenAsync(SocialProvider provider, string token)
            => Task.FromResult(ExternalAuthResult.Fail(ExternalAuthErrorType.Error, string.Empty));
        public bool SupportsProvider(SocialProvider provider) => _supports;
    }

    [Fact]
    public void Validator_Fails_WhenProviderUnsupported()
    {
        var validator = new ExternalLoginRequestValidator(new StubAuthService(false));
        var model = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = "tok" };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Provider)
            .WithErrorMessage("Unsupported provider");
    }

    [Fact]
    public void Validator_Fails_WhenTokenEmpty()
    {
        var validator = new ExternalLoginRequestValidator(new StubAuthService(true));
        var model = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = string.Empty };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validator_Passes_ForValidRequest()
    {
        var validator = new ExternalLoginRequestValidator(new StubAuthService(true));
        var model = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = "tok" };
        var result = validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Provider);
        result.ShouldNotHaveValidationErrorFor(x => x.Token);
    }
}

