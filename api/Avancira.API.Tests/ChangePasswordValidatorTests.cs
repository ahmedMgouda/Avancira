using Avancira.Application.Identity.Users.Constants;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Identity.Users.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    private static ChangePasswordDto CreateDto(string newPassword, string confirmNewPassword) => new()
    {
        Password = "Current1!",
        NewPassword = newPassword,
        ConfirmNewPassword = confirmNewPassword
    };

    [Theory]
    [InlineData("short", UserErrorMessages.PasswordTooShort)]
    [InlineData("alllowercase1!", UserErrorMessages.PasswordRequiresUppercase)]
    [InlineData("ALLUPPERCASE1!", UserErrorMessages.PasswordRequiresLowercase)]
    [InlineData("NoDigits!", UserErrorMessages.PasswordRequiresDigit)]
    [InlineData("NoSymbols1", UserErrorMessages.PasswordRequiresNonAlphanumeric)]
    public void Validator_ProvidesSpecificPasswordErrors(string password, string errorMessage)
    {
        var dto = CreateDto(password, password);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
              .WithErrorMessage(errorMessage);
    }

    [Fact]
    public void Validator_AcceptsStrongPassword()
    {
        var dto = CreateDto("Str0ng!Pass", "Str0ng!Pass");
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
