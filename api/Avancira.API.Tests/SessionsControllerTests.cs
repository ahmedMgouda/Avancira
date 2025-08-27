using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
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

    [Fact]
    public async Task GetSessions_ReturnsSessionsIncludingLastRefreshUtc()
    {
        var mediator = new Mock<ISender>();
        var sessionService = new Mock<ISessionService>();
        var currentUser = new Mock<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.Setup(c => c.GetUserId()).Returns(userId);

        var dto = new SessionDto(
            Guid.NewGuid(),
            "device",
            null,
            null,
            "127.0.0.1",
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

        mediator.Setup(m => m.Send(It.IsAny<GetSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SessionDto> { dto });

        var controller = new SessionsController(mediator.Object, sessionService.Object, currentUser.Object);
        var result = await controller.GetSessions(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
        value.Should().ContainSingle();
        value[0].LastRefreshUtc.Should().Be(dto.LastRefreshUtc);
    }
}
