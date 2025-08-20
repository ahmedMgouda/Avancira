using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Audit;
using Avancira.Application.Caching;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Jobs;
using Avancira.Application.Storage;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class UserRegistrationServiceTests
{
    private static IUserService CreateService(Mock<UserManager<User>> userManager)
    {
        var signInManager = new Mock<SignInManager<User>>(userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null);
        var roleManager = new Mock<RoleManager<Role>>(new Mock<IRoleStore<Role>>().Object, null, null, null, null);
        var cache = new Mock<ICacheService>().Object;
        var jobService = new Mock<IJobService>().Object;
        var notification = new Mock<INotificationService>().Object;
        var storage = new Mock<IStorageService>().Object;
        var config = new ConfigurationBuilder().Build();
        var linkBuilderType = typeof(User).Assembly.GetType("Avancira.Infrastructure.Identity.Users.Services.IdentityLinkBuilder");
        var linkBuilder = Activator.CreateInstance(linkBuilderType!, config);
        var serviceType = typeof(User).Assembly.GetType("Avancira.Infrastructure.Identity.Users.Services.UserService");
        return (IUserService)Activator.CreateInstance(serviceType!, userManager.Object, signInManager.Object, roleManager.Object, null!, cache, jobService, notification, storage, linkBuilder!)!;
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenEmailExists()
    {
        var userManager = MockUserManager();
        userManager.Setup(x => x.FindByEmailAsync("existing@example.com")).ReturnsAsync(new User { Id = "1", Email = "existing@example.com" });

        var service = CreateService(userManager);
        var dto = new RegisterUserDto { Email = "existing@example.com", UserName = "newuser", FirstName = "first", LastName = "last", Password = "P@ssw0rd", ConfirmPassword = "P@ssw0rd", AcceptTerms = true };

        Func<Task> act = () => service.RegisterAsync(dto, "http://origin", CancellationToken.None);

        await act.Should().ThrowAsync<AvanciraException>().WithMessage("Email already in use");
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenUsernameExists()
    {
        var userManager = MockUserManager();
        userManager.Setup(x => x.FindByEmailAsync("new@example.com")).ReturnsAsync((User?)null);
        userManager.Setup(x => x.FindByNameAsync("existing")).ReturnsAsync(new User { Id = "1", UserName = "existing" });

        var service = CreateService(userManager);
        var dto = new RegisterUserDto { Email = "new@example.com", UserName = "existing", FirstName = "first", LastName = "last", Password = "P@ssw0rd", ConfirmPassword = "P@ssw0rd", AcceptTerms = true };

        Func<Task> act = () => service.RegisterAsync(dto, "http://origin", CancellationToken.None);

        await act.Should().ThrowAsync<AvanciraException>().WithMessage("Username already in use");
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
}

public class UsersControllerRegisterTests
{
    [Theory]
    [InlineData("Email already in use")]
    [InlineData("Username already in use")]
    public async Task RegisterUser_ReturnsConflict_OnDuplicate(string message)
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.RegisterAsync(It.IsAny<RegisterUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new AvanciraException(message));

        var controller = new UsersController(Mock.Of<IAuditService>(), userService.Object, new ConfigurationBuilder().Build());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.RegisterUser(new RegisterUserDto { AcceptTerms = true }, CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(message, conflict.Value);
    }

    [Fact]
    public async Task RegisterUser_ReturnsCreated_OnSuccess()
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.RegisterAsync(It.IsAny<RegisterUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new RegisterUserResponseDto("new-user-id"));

        var controller = new UsersController(Mock.Of<IAuditService>(), userService.Object, new ConfigurationBuilder().Build());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.RegisterUser(new RegisterUserDto { AcceptTerms = true }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var response = Assert.IsType<RegisterUserResponseDto>(created.Value);
        Assert.Equal("new-user-id", response.UserId);
    }
}
