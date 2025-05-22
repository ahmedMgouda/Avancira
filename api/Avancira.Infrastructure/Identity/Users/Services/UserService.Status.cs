using Avancira.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Avancira.Domain.Common.Exceptions;

namespace Avancira.Infrastructure.Identity.Users.Services;

internal sealed partial class UserService
{
    public async Task SetChatStatusAsync(string userId, ChatStatus status)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        _ = user ?? throw new NotFoundException("user not found");
        user.Status = status;
        await userManager.UpdateAsync(user);
    }
}
