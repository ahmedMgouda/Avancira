using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Users.Abstractions;
using FluentAssertions;
using MediatR;
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
        var currentUser = new Mock<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.Setup(c => c.GetUserId()).Returns(userId);

        var controller = new SessionsController(mediator.Object, sessionService.Object, currentUser.Object);

        var sessions = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var result = await controller.RevokeSessions(sessions, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        sessionService.Verify(s => s.RevokeSessionsAsync(userId.ToString(), sessions), Times.Once);
    }
}
