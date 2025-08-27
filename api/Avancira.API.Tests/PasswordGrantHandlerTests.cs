using System.Collections.Generic;
using System.Security.Claims;
using Avancira.Application.Identity;
using Avancira.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Xunit;
using static OpenIddict.Server.OpenIddictServerEvents;

public class PasswordGrantHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_SetsPrincipalAndHandles()
    {
        var user = new IdentityUser();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var authService = new Mock<IUserAuthenticationService>();
        authService.Setup(x => x.ValidateCredentialsAsync("test@example.com", "pwd"))
            .ReturnsAsync(user);
        authService.Setup(x => x.CreatePrincipalAsync(user)).ReturnsAsync(principal);

        var logger = new TestLogger<PasswordGrantHandler>();
        var handler = new PasswordGrantHandler(authService.Object, logger);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.Password,
            Username = "test@example.com",
            Password = "pwd",
            Scope = "openid profile"
        };
        var transaction = new OpenIddictServerTransaction
        {
            Request = request
        };
        var context = new HandleTokenRequestContext(transaction);

        await handler.HandleAsync(context);

        context.Principal.Should().Be(principal);
        context.IsHandled.Should().BeTrue();
        logger.Logs.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_InvalidCredentials_LogsWarningAndRejects()
    {
        var authService = new Mock<IUserAuthenticationService>();
        authService.Setup(x => x.ValidateCredentialsAsync("user", "bad"))
            .ReturnsAsync((IdentityUser?)null);

        var logger = new TestLogger<PasswordGrantHandler>();
        var handler = new PasswordGrantHandler(authService.Object, logger);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.Password,
            Username = "user",
            Password = "bad",
            Scope = "openid"
        };
        var transaction = new OpenIddictServerTransaction
        {
            Request = request
        };
        var context = new HandleTokenRequestContext(transaction);

        await handler.HandleAsync(context);

        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.InvalidGrant);
        logger.Logs.Should().Contain(x => x.LogLevel == LogLevel.Warning && x.Message.Contains("Invalid credentials"));
    }

    [Fact]
    public async Task HandleAsync_MissingScope_LogsWarningAndRejects()
    {
        var authService = new Mock<IUserAuthenticationService>();
        var logger = new TestLogger<PasswordGrantHandler>();
        var handler = new PasswordGrantHandler(authService.Object, logger);

        var request = new OpenIddictRequest
        {
            GrantType = OpenIddictConstants.GrantTypes.Password,
            Username = "user",
            Password = "pwd"
        };
        var transaction = new OpenIddictServerTransaction
        {
            Request = request
        };
        var context = new HandleTokenRequestContext(transaction);

        await handler.HandleAsync(context);

        context.IsRejected.Should().BeTrue();
        context.Error.Should().Be(OpenIddictConstants.Errors.InvalidRequest);
        logger.Logs.Should().Contain(x => x.LogLevel == LogLevel.Warning && x.Message.Contains("scope"));
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel LogLevel, string Message)> Logs { get; } = new();
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Logs.Add((logLevel, formatter(state, exception)));
        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
