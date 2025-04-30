using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Microsoft.AspNetCore.Http;

public interface IUserService
{
    // Create
    Task<(bool IsSuccess, string Error)> RegisterUserAsync(RegisterViewModel model, string country);
    Task<SocialLoginResult> SocialLoginAsync(string provider, string token, string? country = null, string? referralToken = null);
    Task<(bool IsSuccess, string Error)> ConfirmEmailAsync(string userId, string token);
    // Read
    Task<(string Token, List<string> Roles)?> LoginUserAsync(LoginViewModel model);
    Task<string?> GetPaymentGatewayAsync(string userId);
    Task<UserDto?> GetUserAsync(string userId);
    Task<UserDto?> GetUserByReferralTokenAsync(string recommendationToken);
    Task<UserDiplomaStatus> GetDiplomaStatusAsync(string userId);
    Task<decimal> GetCompensationPercentageAsync(string userId);
    Task<UserPaymentSchedule?> GetPaymentScheduleAsync(string userId);
    Task<List<object>> GetLandingPageUsersAsync();
    // Update
    Task<bool> ModifyUserAsync(string userId, UserDto updatedUser);
    Task<bool> SendPasswordResetEmail(string email);
    Task<bool> ResetPassword(string email, string token, string newPassword);
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    Task<bool> ModifyPaymentScheduleAsync(string userId, UserPaymentSchedule paymentSchedule);
    Task UpdateCompensationPercentageAsync(string userId, int newPercentage);
    Task<bool> SubmitDiplomaAsync(string userId, IFormFile diplomaFile, string? diplomaDescription = null);
    // Delete
    Task<bool> DeleteAccountAsync(string userId);
}

