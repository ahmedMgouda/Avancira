using Avancira.Application.Identity.Users.Constants;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Identity.Users.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Theory]
    [InlineData("short", UserErrorMessages.PasswordTooShort)]
    [InlineData("alllowercase1!", UserErrorMessages.PasswordRequiresUppercase)]
    [InlineData("ALLUPPERCASE1!", UserErrorMessages.PasswordRequiresLowercase)]
    [InlineData("NoDigits!", UserErrorMessages.PasswordRequiresDigit)]
    [InlineData("NoSymbols1", UserErrorMessages.PasswordRequiresNonAlphanumeric)]
    public void Validator_ProvidesSpecificPasswordErrors(string password, string errorMessage)
    {
        var dto = new ResetPasswordDto
        {
            UserId = "user-id",
            Password = password,
            ConfirmPassword = password,
            Token = "token"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage(errorMessage);
    }

    [Fact]
    public void Validator_Rejects_When_Passwords_Do_Not_Match()
    {
        var dto = new ResetPasswordDto
        {
            UserId = "user-id",
            Password = "Str0ng!Pass",
            ConfirmPassword = "Different1!",
            Token = "token"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
              .WithErrorMessage(UserErrorMessages.PasswordsDoNotMatch);
    }

    [Fact]
    public void Validator_AcceptsStrongPassword()
    {
        var dto = new ResetPasswordDto
        {
            UserId = "user-id",
            Password = "Str0ng!Pass",
            ConfirmPassword = "Str0ng!Pass",
            Token = "token"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
