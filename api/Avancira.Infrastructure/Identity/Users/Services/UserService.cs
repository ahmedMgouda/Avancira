﻿using Avancira.Application.Caching;
using Avancira.Application.Catalog;
using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Jobs;
using Avancira.Application.Mail;
using Avancira.Application.Storage;
using Avancira.Application.Storage.File;
using Avancira.Application.Storage.File.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Infrastructure.Constants;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Persistence;
using Avancira.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text;
using Avancira.Domain.Common.Exceptions;
using Microsoft.Extensions.Options;
using Avancira.Application.Auth.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Avancira.Infrastructure.Auth.Jwt;

namespace Avancira.Infrastructure.Identity.Users.Services;
internal sealed partial class UserService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    RoleManager<Role> roleManager,
    AvanciraDbContext db,
    ICacheService cache,
    IJobService jobService,
    INotificationService notificationService,
    IStorageService storageService,
    IOptions<JwtOptions> jwtOptions
    ) : Avancira.Application.Identity.Users.Abstractions.IUserService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new AvanciraException("An error occurred while confirming E-Mail.");

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded
            ? string.Format("Account Confirmed for E-Mail {0}. You can now use the /api/tokens endpoint to generate JWT.", user.Email)
            : throw new AvanciraException(string.Format("An error occurred while confirming {0}", user.Email));
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        return await userManager.FindByEmailAsync(email.Normalize()) is User user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is User user && user.Id != exceptId;
    }

    public async Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .Include(u => u.Country)
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        return user.Adapt<UserDetailDto>();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        return users.Adapt<List<UserDetailDto>>();
    }

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserDto request, string origin, CancellationToken cancellationToken)
    {
        // create user entity
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            TimeZoneId = request.TimeZoneId,
            IsActive = true,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
        };

        // register user
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new AvanciraException("error while registering a new user", errors);
        }

        // add basic role
        await userManager.AddToRoleAsync(user, AvanciraRoles.Basic);

        // send confirmation mail
        if (!string.IsNullOrEmpty(user.Email))
        {
            string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
            var confirmEmailEvent = new ConfirmEmailEvent
            {
                UserId = user.Id,
                Email = user.Email,
                ConfirmationLink = emailVerificationUri
            };

            // Use notification service to send email confirmation
            await notificationService.NotifyAsync(NotificationEvent.ConfirmEmail, confirmEmailEvent);
        }

        return new RegisterUserResponseDto(user.Id);
    }

    public async Task ToggleStatusAsync(ToggleUserStatusDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.Where(u => u.Id == request.UserId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("User Not Found.");

        bool isAdmin = await userManager.IsInRoleAsync(user, AvanciraRoles.Admin);
        if (isAdmin)
        {
            throw new AvanciraException("Administrators Profile's Status cannot be toggled");
        }

        user.IsActive = request.ActivateUser;

        await userManager.UpdateAsync(user);
    }

    public async Task UpdateAsync(UpdateUserDto request, string userId)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);

        _ = user ?? throw new NotFoundException("user not found");

        // Handle image upload/deletion
        Uri imageUri = user.ImageUrl ?? null!;
        if (request.Image != null || request.DeleteCurrentImage)
        {
            FileUploadDto? fileUploadDto = null;
            if (request.Image != null)
            {
                using var memoryStream = new MemoryStream();
                await request.Image.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                var base64Data = Convert.ToBase64String(fileBytes);
                var base64String = $"data:{request.Image.ContentType};base64,{base64Data}";

                fileUploadDto = new FileUploadDto
                {
                    Name = request.Image.FileName,
                    Extension = Path.GetExtension(request.Image.FileName),
                    Data = base64String
                };
            }
            
            user.ImageUrl = await storageService.UploadAsync<User>(fileUploadDto, FileType.Image);
            if (request.DeleteCurrentImage && imageUri != null)
            {
                storageService.Remove(imageUri);
            }
        }

        // Update basic user fields
        if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.Bio)) user.Bio = request.Bio;
        if (!string.IsNullOrEmpty(request.TimeZoneId)) user.TimeZoneId = request.TimeZoneId;
        
        // Parse DateOfBirth from string
        if (!string.IsNullOrEmpty(request.DateOfBirth))
        {
            if (DateOnly.TryParse(request.DateOfBirth, out var dateOfBirth))
            {
                user.DateOfBirth = dateOfBirth;
            }
        }
        
        if (!string.IsNullOrEmpty(request.SkypeId)) user.SkypeId = request.SkypeId;
        if (!string.IsNullOrEmpty(request.HangoutId)) user.HangoutId = request.HangoutId;
        
        // Handle phone number update
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber;
            string? phoneNumber = await userManager.GetPhoneNumberAsync(user);
            if (request.PhoneNumber != phoneNumber)
            {
                await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            }
        }

        // Handle address fields - Check if address exists to avoid unique constraint violation
        var existingAddress = await db.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
        
        if (existingAddress == null)
        {
            // Create new address only if it doesn't exist
            user.Address = new Avancira.Infrastructure.Catalog.Address
            {
                UserId = userId
            };
        }
        else
        {
            // Use existing address
            user.Address = existingAddress;
        }

        // Update address fields
        if (!string.IsNullOrEmpty(request.AddressFormattedAddress)) user.Address.FormattedAddress = request.AddressFormattedAddress;
        if (!string.IsNullOrEmpty(request.AddressStreetAddress)) user.Address.StreetAddress = request.AddressStreetAddress;
        if (!string.IsNullOrEmpty(request.AddressCity)) user.Address.City = request.AddressCity;
        if (!string.IsNullOrEmpty(request.AddressState)) user.Address.State = request.AddressState;
        if (!string.IsNullOrEmpty(request.AddressCountry)) user.Address.Country = request.AddressCountry;
        if (!string.IsNullOrEmpty(request.AddressPostalCode)) user.Address.PostalCode = request.AddressPostalCode;
        if (request.AddressLatitude.HasValue) user.Address.Latitude = request.AddressLatitude.Value;
        if (request.AddressLongitude.HasValue) user.Address.Longitude = request.AddressLongitude.Value;

        // Note: The following fields don't exist on the User entity, so we'll skip them for now
        // If you need these fields, you'll need to add them to the User entity first:
        // - ProfileVerified, LessonsCompleted, Evaluations, RecommendationToken, IsStripeConnected
        
        // You can add these to the User entity if needed:
        // if (!string.IsNullOrEmpty(request.ProfileVerified)) user.ProfileVerified = request.ProfileVerified;
        // if (request.LessonsCompleted.HasValue) user.LessonsCompleted = request.LessonsCompleted.Value;
        // if (request.Evaluations.HasValue) user.Evaluations = request.Evaluations.Value;
        // if (!string.IsNullOrEmpty(request.RecommendationToken)) user.RecommendationToken = request.RecommendationToken;
        // if (request.IsStripeConnected.HasValue) user.IsStripeConnected = request.IsStripeConnected.Value;

        var result = await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        if (!result.Succeeded)
        {
            throw new AvanciraException("Update profile failed");
        }
    }

    public async Task DeleteAsync(string userId)
    {
        User? user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        user.IsActive = false;
        IdentityResult? result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            List<string> errors = result.Errors.Select(error => error.Description).ToList();
            throw new AvanciraException("Delete profile failed", errors);
        }
    }

    private async Task<string> GetEmailVerificationUriAsync(User user, string origin)
    {
        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        return verificationUri;
    }

    public async Task<string> AssignRolesAsync(string userId, AssignUserRoleDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        // Check if the user is an admin for which the admin role is getting disabled
        if (await userManager.IsInRoleAsync(user, AvanciraRoles.Admin)
            && request.UserRoles.Exists(a => !a.Enabled && a.RoleName == AvanciraRoles.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = (await userManager.GetUsersInRoleAsync(AvanciraRoles.Admin)).Count;

            // Ensure at least 2 admins exist in the tenant
            if (adminCount <= 2)
            {
                throw new AvanciraException("System should have at least 2 admins.");
            }
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await roleManager.FindByNameAsync(userRole.RoleName!) is not null)
            {
                if (userRole.Enabled)
                {
                    if (!await userManager.IsInRoleAsync(user, userRole.RoleName!))
                    {
                        await userManager.AddToRoleAsync(user, userRole.RoleName!);
                    }
                }
                else
                {
                    await userManager.RemoveFromRoleAsync(user, userRole.RoleName!);
                }
            }
        }

        return "User Roles Updated Successfully.";
    }

    public async Task<List<UserRoleDetailDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDetailDto>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new NotFoundException("user not found");
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new NotFoundException("roles not found");
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDetailDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await userManager.IsInRoleAsync(user, role.Name!)
            });
        }

        return userRoles;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedException("Invalid email or password.");

        //if (!user.EmailConfirmed)
        //    throw new AvanciraException("Email not confirmed.");

        //if (!user.IsActive)
        //    throw new AvanciraException("User account is disabled.");

        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var userRoles = await userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            userClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer: JwtAuthConstants.Issuer,
            audience: JwtAuthConstants.Audience,
            claims: userClaims,
            expires: expires,
            signingCredentials: creds
        );

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expires,
            Roles = userRoles
        };
    }

}
