using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Avancira.Infrastructure.Identity.Users.Services;

internal sealed partial class UserService
{
    public async Task ForgotPasswordAsync(ForgotPasswordDto request, string origin, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AvanciraException("Invalid password reset request.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || string.IsNullOrWhiteSpace(user.Email) || !(await userManager.IsEmailConfirmedAsync(user)))
        {
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(origin))
            throw new AvanciraException("Password reset is temporarily unavailable.");

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            throw new AvanciraException("Password reset is temporarily unavailable.");

        var allowedOrigins = config.GetSection("CorsOptions:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        bool isAllowedOrigin = allowedOrigins.Any(allowedOrigin =>
            Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var allowedUri) &&
            string.Equals(allowedUri.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(allowedUri.Host, originUri.Host, StringComparison.OrdinalIgnoreCase) &&
            allowedUri.Port == originUri.Port);

        if (!isAllowedOrigin)
            throw new AvanciraException("Password reset is temporarily unavailable.");

        var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        var sanitizedOrigin = originUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        var resetPasswordUri = BuildResetPasswordLink(sanitizedOrigin, request.Email, encodedToken);

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
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            throw new AvanciraException("Invalid password reset request.");

        if (string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword) ||
            request.Password != request.ConfirmPassword)
            throw new AvanciraException("Passwords do not match.");

        const string invalidRequestMessage = "Invalid password reset request.";
        const string invalidTokenMessage = "Invalid password reset token.";

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new AvanciraException(invalidRequestMessage);

        string decodedToken;
        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            throw new AvanciraException(invalidTokenMessage);
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("Error resetting password.", errors);
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
        _ = user ?? throw new NotFoundException("user not found");

        var result = await userManager.ChangePasswordAsync(user, request.Password, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("failed to change password", errors);
        }

        await userManager.UpdateSecurityStampAsync(user);
    }

    private static string BuildResetPasswordLink(string origin, string email, string encodedToken)
    {
        var baseUri = origin.TrimEnd('/');
        var endpoint = $"{baseUri}/reset-password";
        var withEmail = QueryHelpers.AddQueryString(endpoint, "email", email);
        var withToken = QueryHelpers.AddQueryString(withEmail, "token", encodedToken);
        return withToken;
    }
}
