using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Identity.Users.Validators;
using FluentValidation.TestHelper;
using Xunit;

public class ResetPasswordValidatorTests
{
    private readonly ResetPasswordValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("Short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigits!")]
    [InlineData("NoSymbols1")]
    public void Validator_RejectsWeakPasswords(string password)
    {
        var dto = new ResetPasswordDto
        {
            UserId = "user-id",
            Password = password,
            Token = "token"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validator_AcceptsStrongPassword()
    {
        var dto = new ResetPasswordDto
        {
            UserId = "user-id",
            Password = "Str0ng!Pass",
            Token = "token"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
