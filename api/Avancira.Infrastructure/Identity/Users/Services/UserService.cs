using Avancira.Application.Caching;
using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Jobs;
using Avancira.Application.Storage;
using Avancira.Application.Storage.File;
using Avancira.Application.Storage.File.Dtos;
using System;
using System.Security.Claims;
using System.Text;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;
using Avancira.Infrastructure.Constants;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Persistence;
using Avancira.Shared.Authorization;
using Avancira.Shared.Constants;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Avancira.Infrastructure.Identity.Users.Services;
internal sealed partial class UserService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    AvanciraDbContext db,
    ICacheService cache,
    IJobService jobService,
    INotificationService notificationService,
    IStorageService storageService,
    IdentityLinkBuilder linkBuilder
    ) : Application.Identity.Users.Abstractions.IUserService
{
    public async Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new AvanciraException("Invalid email confirmation request.");

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch
        {
            throw new AvanciraException("Invalid email confirmation token.");
        }

        var result = await userManager.ConfirmEmailAsync(user, decoded);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("Error confirming email.", errors);
        }

        user.IsActive = true;
        await userManager.UpdateAsync(user);

        return $"Account confirmed for {user.Email}.";
    }


    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        var u = await userManager.FindByEmailAsync(email);
        return u is User user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        var sanitized = phoneNumber
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return await userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == sanitized) is User user && user.Id != exceptId;
    }

    public async Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .Include(u => u.Country)
            .Include(u => u.TutorProfile)
            .Include(u => u.StudentProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        _ = user ?? throw new AvanciraNotFoundException("user not found");

        return user.Adapt<UserDetailDto>();
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
     => userManager.Users.AsNoTracking().CountAsync(cancellationToken);

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
        if (await ExistsWithEmailAsync(request.Email))
            throw new AvanciraException("Email already in use");

        if (await ExistsWithNameAsync(request.UserName))
            throw new AvanciraException("Username already in use");

        var normalizedCountryCode = request.CountryCode.ToUpperInvariant();
        bool countryExists = await db.Countries.AnyAsync(c => c.Code == normalizedCountryCode, cancellationToken);
        if (!countryExists)
        {
            throw new AvanciraNotFoundException($"Country '{request.CountryCode}' not found.");
        }

        var strategy = db.Database.CreateExecutionStrategy();

        RegisterUserResponseDto response = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var sanitizedPhone = request.PhoneNumber
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal);

                var user = new User
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    UserName = request.UserName,
                    PhoneNumber = sanitizedPhone,
                    TimeZoneId = request.TimeZoneId,
                    CountryCode = normalizedCountryCode,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    IsActive = false,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                    throw new AvanciraException("Error while registering user", result.Errors.Select(e => e.Description));

                var rolesToAssign = new List<string>();
                if (request.RegisterAsStudent)
                {
                    rolesToAssign.Add(SeedDefaults.Roles.Student);
                }

                if (request.RegisterAsTutor)
                {
                    rolesToAssign.Add(SeedDefaults.Roles.Tutor);
                }

                foreach (var role in rolesToAssign.Distinct())
                {
                    var roleResult = await userManager.AddToRoleAsync(user, role);
                    if (!roleResult.Succeeded)
                        throw new AvanciraException("Error while assigning user role", roleResult.Errors.Select(e => e.Description));
                }

                if (request.RegisterAsStudent)
                {
                    db.StudentProfiles.Add(StudentProfile.Create(user.Id));
                }

                if (request.RegisterAsTutor)
                {
                    var tutorProfile = TutorProfile.Create(user.Id);
                    tutorProfile.UpdateOverview(string.Empty, string.Empty, 0, null, null, null);
                    db.TutorProfiles.Add(tutorProfile);
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // send confirmation mail after commit
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
                        var confirmEmailEvent = new ConfirmEmailEvent
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            ConfirmationLink = emailVerificationUri
                        };

                        await notificationService.NotifyAsync(NotificationEvent.ConfirmEmail, confirmEmailEvent);
                    }
                    catch
                    {
                        // swallow notification errors
                    }
                }

                response = new RegisterUserResponseDto(user.Id);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return response;
    }
  
    public async Task ToggleStatusAsync(ToggleUserStatusDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        _ = user ?? throw new AvanciraNotFoundException("User Not Found.");

        bool isAdmin = await userManager.IsInRoleAsync(user, SeedDefaults.Roles.Admin);
        if (isAdmin)
            throw new AvanciraException("Administrators Profile's Status cannot be toggled");

        user.IsActive = request.ActivateUser;
        await userManager.UpdateAsync(user);
    }

    public async Task UpdateAsync(UpdateUserDto request, string userId)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);

        _ = user ?? throw new AvanciraNotFoundException("user not found");

        // Handle image upload/deletion
        var imageUri = user.ProfileImageUrl;
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
            
            if (fileUploadDto is not null)
            {
                user.ProfileImageUrl = await storageService.UploadAsync<User>(fileUploadDto, FileType.Image);
            }
            else if (request.DeleteCurrentImage)
            {
                user.ProfileImageUrl = null;
            }

            if (request.DeleteCurrentImage && imageUri is not null)
            {
                storageService.Remove(imageUri);
            }
        }

        // Update basic user fields
        if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.TimeZoneId)) user.TimeZoneId = request.TimeZoneId;
        
        // Parse DateOfBirth from string
        if (!string.IsNullOrEmpty(request.DateOfBirth))
        {
            if (DateOnly.TryParse(request.DateOfBirth, out var dateOfBirth))
            {
                user.DateOfBirth = dateOfBirth;
            }
        }
        
        // Handle phone number update
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var sanitizedPhone = request.PhoneNumber
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal);

            user.PhoneNumber = sanitizedPhone;

            string? currentPhone = await userManager.GetPhoneNumberAsync(user);
            if (!string.Equals(currentPhone, sanitizedPhone, StringComparison.Ordinal))
            {
                await userManager.SetPhoneNumberAsync(user, sanitizedPhone);
            }
        }

        user.Address ??= new Address();

        if (!string.IsNullOrEmpty(request.AddressStreetAddress))
        {
            user.Address.Street = request.AddressStreetAddress;
        }

        if (!string.IsNullOrEmpty(request.AddressCity))
        {
            user.Address.City = request.AddressCity;
        }

        if (!string.IsNullOrEmpty(request.AddressState))
        {
            user.Address.State = request.AddressState;
        }

        if (!string.IsNullOrEmpty(request.AddressPostalCode))
        {
            user.Address.PostalCode = request.AddressPostalCode;
        }

        // Note: The following fields don't exist on the User entity, so we'll skip them for now
        // If you need these fields, you'll need to add them to the User entity first:
        // - ProfileVerified, RecommendationToken, IsStripeConnected
        
        // You can add these to the User entity if needed:
        // if (!string.IsNullOrEmpty(request.ProfileVerified)) user.ProfileVerified = request.ProfileVerified;
        // if (!string.IsNullOrEmpty(request.RecommendationToken)) user.RecommendationToken = request.RecommendationToken;
        // if (request.IsStripeConnected.HasValue) user.IsStripeConnected = request.IsStripeConnected.Value;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new AvanciraException("Update profile failed");
        }
    }

    public async Task DeleteAsync(string userId)
    {
        User? user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new AvanciraNotFoundException("User Not Found.");

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
        var sanitizedOrigin = linkBuilder.ValidateOrigin(origin, "Email verification is temporarily unavailable.");
        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri(string.Concat($"{sanitizedOrigin}/", route));
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        return verificationUri;
    }

    public async Task<string> AssignRolesAsync(string userId, AssignUserRoleDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new AvanciraNotFoundException("user not found");

        // Check if the user is an admin for which the admin role is getting disabled
        if (await userManager.IsInRoleAsync(user, SeedDefaults.Roles.Admin)
            && request.UserRoles.Exists(a => !a.Enabled && a.RoleName == SeedDefaults.Roles.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = (await userManager.GetUsersInRoleAsync(SeedDefaults.Roles.Admin)).Count;

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
        if (user is null) throw new AvanciraNotFoundException("user not found");
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new AvanciraNotFoundException("roles not found");
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
}
