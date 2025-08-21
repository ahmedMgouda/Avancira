using Avancira.Application.Identity.Users.Constants;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Identity.Users.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    private static RegisterUserDto CreateDto(string password, string confirmPassword) => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        UserName = "johndoe",
        Password = password,
        ConfirmPassword = confirmPassword,
        AcceptTerms = true
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
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage(errorMessage);
    }

    [Fact]
    public void Validator_AcceptsStrongPassword()
    {
        var dto = CreateDto("Str0ng!Pass", "Str0ng!Pass");
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
