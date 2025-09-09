using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task RevokeSessions_UsesMediatorAndReturnsNoContent()
    {
        var mediator = new Mock<ISender>();
        var currentUser = new Mock<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.Setup(c => c.GetUserId()).Returns(userId);
        mediator.Setup(m => m.Send(It.IsAny<RevokeSessionsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
        var controller = new SessionsController(mediator.Object, currentUser.Object);

        var sessions = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var result = await controller.RevokeSessions(sessions, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        mediator.Verify(m => m.Send(
            It.Is<RevokeSessionsCommand>(c => c.UserId == userId.ToString() && c.SessionIds.SequenceEqual(sessions)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessions_ReturnsSessionsIncludingLastRefreshUtc()
    {
        var mediator = new Mock<ISender>();
        var currentUser = new Mock<ICurrentUser>();
        var userId = Guid.NewGuid();
        currentUser.Setup(c => c.GetUserId()).Returns(userId);

        var dto = new SessionDto(
            Guid.NewGuid(),
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
        var controller = new SessionsController(mediator.Object, currentUser.Object);
        var result = await controller.GetSessions(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
        value.Should().ContainSingle();
        value[0].LastRefreshUtc.Should().Be(dto.LastRefreshUtc);
    }
}
