using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Identity.Users.Constants;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Avancira.Infrastructure.Identity.Users.Services;

internal sealed partial class UserService
{
    public async Task ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || string.IsNullOrWhiteSpace(user.Email) || !(await userManager.IsEmailConfirmedAsync(user)))
        {
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            return;
        }

        var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        var resetPasswordUri = linkBuilder.BuildResetPasswordLink(user.Id, encodedToken);

        var resetPasswordEvent = new ResetPasswordEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            ResetPasswordLink = resetPasswordUri
        };

        await notificationService.NotifyAsync(NotificationEvent.ResetPassword, resetPasswordEvent);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
            throw new AvanciraException(UserErrorMessages.InvalidPasswordResetRequest);

        string decodedToken;
        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            throw new AvanciraException(UserErrorMessages.InvalidPasswordResetToken);
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException(UserErrorMessages.ErrorResettingPassword, errors);
        }

        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedException();

        if (request is null || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new AvanciraException("Current and new passwords are required.");

        var user = await userManager.FindByIdAsync(userId);
        _ = user ?? throw new AvanciraNotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, request.Password, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("failed to change password", errors);
        }

        await userManager.UpdateSecurityStampAsync(user);
    }

}
