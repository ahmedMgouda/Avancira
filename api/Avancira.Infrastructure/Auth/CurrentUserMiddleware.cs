﻿using Avancira.Application.Identity.Users.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Avancira.Infrastructure.Auth;
public class CurrentUserMiddleware(ICurrentUserInitializer currentUserInitializer) : IMiddleware
{
    private readonly ICurrentUserInitializer _currentUserInitializer = currentUserInitializer;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _currentUserInitializer.SetCurrentUser(context.User);
        await next(context);
    }
}
