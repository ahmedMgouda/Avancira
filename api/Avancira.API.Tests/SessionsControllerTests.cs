using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Identity.Tokens;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class SessionsControllerTests
{
    [Fact]
    public async Task RevokeSessions_UsesServiceAndReturnsNoContent()
    {
        var mediator = new Mock<ISender>();
        var sessionService = new Mock<ISessionService>();
        var controller = new SessionsController(mediator.Object, sessionService.Object);

        var userId = "user-123";
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var sessions = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var result = await controller.RevokeSessions(sessions, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sessionService.Verify(s => s.RevokeSessionsAsync(userId, sessions), Times.Once);
    }
}
