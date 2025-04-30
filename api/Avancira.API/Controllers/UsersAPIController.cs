//using System;
//using System.Threading.Tasks;
//using Avancira.Application.Catalog.Dtos;
//using Avancira.Application.Identity.Users.Dtos;
//using Backend.Controllers;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//[Route("api/users")]
//[ApiController]
//public class UsersAPIController : BaseController
//{
//    private readonly IUserService _userService;

//    public UsersAPIController(
//        IUserService userService
//    )
//    {
//        _userService = userService;
//    }

//    // Create
//    [HttpPost("register")]
//    public async Task<IActionResult> Register(RegisterViewModel model)
//    {
//        var country = HttpContext.Items["Country"]?.ToString() ?? "AU";
//        var (isSuccess, error) = await _userService.RegisterUserAsync(model, country);
//        if (!isSuccess)
//        {
//            return JsonError("Registration failed", error);
//        }
//        return JsonOk();
//    }

//    [HttpPost("social-login")]
//    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
//    {
//        var country = HttpContext.Items["Country"]?.ToString() ?? "AU";

//        if (string.IsNullOrEmpty(request.Provider) || string.IsNullOrEmpty(request.Token))
//        {
//            return BadRequest(new { Message = "Provider and Token are required." });
//        }

//        try
//        {
//            // Delegate social login handling to the service layer
//            var result = await _userService.SocialLoginAsync(request.Provider, request.Token, country);

//            if (result == null)
//            {
//                return Unauthorized(new { Message = "Social login failed." });
//            }

//            return Ok(new { token = result.Token, roles = result.Roles, isRegistered = result.isRegistered });
//        }
//        catch (Exception ex)
//        {
//            return Unauthorized(new { Message = ex.Message });
//        }
//    }

//    [HttpGet("ConfirmEmail")]
//    public async Task<IActionResult> ConfirmEmail(string userId, string token)
//    {
//        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
//        {
//            return JsonError("Invalid request: Missing user ID or token.");
//        }

//        try
//        {
//            var result = await _userService.ConfirmEmailAsync(userId, token);

//            if (result.IsSuccess)
//            {
//                return JsonOk();
//            }
//            else
//            {
//                Console.WriteLine($"Email confirmation error: {result.Error}");
//                return JsonError("Email confirmation failed.", result.Error);
//            }
//        }
//        catch (Exception ex)
//        {
//            // Log the exception (use a proper logger in production)
//            Console.WriteLine($"An exception occurred during email confirmation: {ex.Message}");
//            return JsonError("An unexpected error occurred during email confirmation.", ex.Message);
//        }
//    }

//    [HttpPost("login")]
//    public async Task<IActionResult> Login(LoginViewModel model)
//    {
//        var result = await _userService.LoginUserAsync(model);
//        if (result == null)
//        {
//            return Unauthorized();
//        }

//        return JsonOk(new { token = result.Value.Token, roles = result.Value.Roles });
//    }

//    // Read
//    [Authorize]
//    [HttpGet("me")]
//    public async Task<IActionResult> GetAsync()
//    {
//        var userId = GetUserId();
//        var user = await _userService.GetUserAsync(userId);
//        if (user == null)
//        {
//            throw new UnauthorizedAccessException("User not found.");
//        }
//        return JsonOk(user);
//    }

//    [HttpGet("by-token/{recommendationToken}")]
//    public async Task<IActionResult> GetUserByToken(string recommendationToken)
//    {
//        var user = await _userService.GetUserByReferralTokenAsync(recommendationToken);
//        if (user == null)
//        {
//            return NotFound("User not found.");
//        }

//        return JsonOk(user);
//    }

//    [Authorize]
//    [HttpGet("payment-schedule")]
//    public async Task<IActionResult> GetPaymentSchedule()
//    {
//        var userId = GetUserId();
//        var paymentSchedule = await _userService.GetPaymentScheduleAsync(userId);

//        if (paymentSchedule == null)
//        {
//            return NotFound("User not found.");
//        }

//        return JsonOk(paymentSchedule.Value);
//    }

//    [Authorize]
//    [HttpGet("diploma-status")]
//    public async Task<IActionResult> GetDiplomaStatus()
//    {
//        var userId = GetUserId();
//        var status = await _userService.GetDiplomaStatusAsync(userId);
//        return JsonOk(new { status });
//    }

//    [Authorize]
//    [HttpGet("compensation-percentage")]
//    public async Task<IActionResult> GetCompensationPercentage()
//    {
//        var userId = GetUserId();
//        var percentage = await _userService.GetCompensationPercentageAsync(userId);
//        return JsonOk(percentage);
//    }



//    // Update
//    [Authorize]
//    [HttpPut("me")]
//    public async Task<IActionResult> UpdateAsync([FromForm] UserDto updatedUser)
//    {
//        var userId = GetUserId();
//        if (!await _userService.ModifyUserAsync(userId, updatedUser))
//        {
//            return NotFound("User not found.");
//        }

//        return JsonOk(new { success = true, message = "Profile updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("compensation-percentage")]
//    public async Task<IActionResult> UpdateCompensationPercentage([FromBody] CompensationUpdateDto dto)
//    {
//        var userId = GetUserId();
//        await _userService.UpdateCompensationPercentageAsync(userId, dto.Percentage);
//        return JsonOk(new { message = "Compensation percentage updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("payment-schedule")]
//    public async Task<IActionResult> UpdatePaymentSchedule([FromBody] PaymentScheduleDto dto)
//    {
//        var userId = GetUserId();
//        var success = await _userService.ModifyPaymentScheduleAsync(userId, dto.PaymentSchedule);

//        if (!success)
//        {
//            return NotFound("User not found or update failed.");
//        }

//        return JsonOk(new { message = "Payment schedule updated successfully." });
//    }

//    [Authorize]
//    [HttpPut("change-password")]
//    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
//    {
//        if (string.IsNullOrEmpty(model.Password) ||
//            string.IsNullOrEmpty(model.NewPassword) ||
//            string.IsNullOrEmpty(model.ConfirmNewPassword))
//        {
//            return JsonError("All fields are required.");
//        }

//        if (model.NewPassword != model.ConfirmNewPassword)
//        {
//            return JsonError("New password and confirmation do not match.");
//        }

//        var userId = GetUserId();
//        var result = await _userService.ChangePasswordAsync(userId, model.Password, model.NewPassword);

//        if (!result)
//        {
//            return JsonError("Password update failed. Please check your old password and try again.");
//        }

//        return JsonOk(new { message = "Password updated successfully." });
//    }

//    [HttpPut("request-reset-password")]
//    public async Task<IActionResult> RequestPasswordReset([FromBody] ResetPasswordRequestDto model)
//    {
//        var success = await _userService.SendPasswordResetEmail(model.Email);
//        if (!success)
//        {
//            return NotFound("User with this email address not found.");
//        }

//        return JsonOk(new { success = true, message = "Password reset link sent to your email." });
//    }

//    [HttpPut("reset-password")]
//    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
//    {
//        var success = await _userService.ResetPassword(model.Email, model.Token, model.Password);
//        if (!success)
//        {
//            return JsonError("Invalid or expired token.");
//        }

//        return JsonOk(new { success = true, message = "Password reset successfully." });
//    }

//    [Authorize]
//    [HttpPut("submit-diploma")]
//    public async Task<IActionResult> SubmitDiploma([FromForm] IFormFile diplomaFile)
//    {
//        var userId = GetUserId();
//        if (diplomaFile == null)
//        {
//            return JsonError("No diploma file provided.");
//        }

//        var success = await _userService.SubmitDiplomaAsync(userId, diplomaFile);
//        return success ? JsonOk(new { message = "Diploma submitted successfully." }) : JsonError("Failed to submit diploma.");
//    }

//    // Delete
//    [Authorize]
//    [HttpDelete("me")]
//    public async Task<IActionResult> DeleteAccount()
//    {
//        var userId = GetUserId();
//        var success = await _userService.DeleteAccountAsync(userId);
//        if (!success)
//        {
//            return NotFound(new { success = false, message = "User not found." });
//        }

//        return JsonOk(new { success = true, message = "Account deleted successfully." });
//    }
//}

