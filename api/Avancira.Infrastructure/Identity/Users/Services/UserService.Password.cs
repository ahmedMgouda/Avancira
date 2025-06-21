using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Mail;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.ObjectModel;
using System.Text;

namespace Avancira.Infrastructure.Identity.Users.Services;
internal sealed partial class UserService
{
    public async Task ForgotPasswordAsync(ForgotPasswordDto request, string origin, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("user not found");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("user email cannot be null or empty");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetPasswordUri = $"{origin}/reset-password?token={token}&email={request.Email}";
        var resetPasswordEvent = new ResetPasswordEvent
        {
            UserId = user.Id,
            Email = user.Email,
            ResetPasswordLink = resetPasswordUri
        };

        // Use notification service to send password reset email
        await notificationService.NotifyAsync(NotificationEvent.ResetPassword, resetPasswordEvent);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("user not found");
        }

        request.Token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("error resetting password", errors);
        }
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, request.Password, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("failed to change password", errors);
        }
    }
}
