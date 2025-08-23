using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Avancira.Application.Auth;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Identity.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

public class ExternalUserServiceTests
{
    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static ExternalLoginInfo CreateLoginInfo(string? email = "user@example.com", string? name = "User Example")
    {
        var claims = new List<Claim>();
        if (email != null) claims.Add(new Claim(ClaimTypes.Email, email));
        if (name != null) claims.Add(new Claim(ClaimTypes.Name, name));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
        return new ExternalLoginInfo(principal, "Google", "123", "Google");
    }

    [Fact]
    public async Task EnsureUserAsync_CreatesUserAndAddsLogin_WhenUserDoesNotExist()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => u.Id = "new-id")
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo();
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be("new-id");
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Once);
        userManager.Verify(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()), Times.Once);
    }

    [Fact]
    public async Task EnsureUserAsync_ReturnsProblem_WhenUserCreationFails()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo();
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ExternalUserError.Problem);
        result.Error.Should().Be("fail");
    }

    [Fact]
    public async Task EnsureUserAsync_ReturnsBadRequest_WhenAddLoginFails()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync((User?)null);
        User? createdUser = null;
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.Id = "new-id"; createdUser = u; })
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "login-fail" }));
        userManager.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo();
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ExternalUserError.BadRequest);
        result.Error.Should().Be("login-fail");
        userManager.Verify(m => m.DeleteAsync(createdUser!), Times.Once);
    }

    [Fact]
    public async Task EnsureUserAsync_SkipsDeletion_WhenAddLoginFailsForExistingUser()
    {
        var existingUser = new User { Id = "existing-id" };
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync(existingUser);
        userManager.Setup(m => m.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "login-fail" }));

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo();
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ExternalUserError.BadRequest);
        result.Error.Should().Be("login-fail");
        userManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task EnsureUserAsync_ReturnsUnauthorized_WhenEmailMissing()
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo(email: null);
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeFalse();
        result.ErrorType.Should().Be(ExternalUserError.Unauthorized);
    }

    [Fact]
    public async Task EnsureUserAsync_ConfirmsEmail_WhenExistingUserEmailVerified()
    {
        var existingUser = new User { Id = "existing-id", EmailConfirmed = false };
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByLoginAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);
        userManager.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync(existingUser);
        userManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        var service = new ExternalUserService(userManager.Object);
        var info = CreateLoginInfo();
        var result = await service.EnsureUserAsync(info);

        result.Succeeded.Should().BeTrue();
        existingUser.EmailConfirmed.Should().BeTrue();
        userManager.Verify(m => m.UpdateAsync(It.Is<User>(u => u.EmailConfirmed)), Times.Once);
    }
}

