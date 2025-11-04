using Avancira.Application.Caching;
using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Jobs;
using Avancira.Application.Storage;
using Avancira.Application.Storage.File;
using Avancira.Application.Storage.File.Dtos;
using Avancira.Application.StudentProfiles.Dtos;
using Avancira.Application.TutorProfiles.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Common.Exceptions;
using Avancira.Domain.Students;
using Avancira.Domain.Tutors;
using Avancira.Domain.Users;
using Avancira.Infrastructure.Constants;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Persistence;
using Avancira.Shared.Authorization;
using Avancira.Shared.Constants;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

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
) : IUserService
{
    // =====================================================================
    //  EXISTENCE CHECKS
    // =====================================================================

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
        => await userManager.FindByEmailAsync(email) is User u && u.Id != exceptId;

    public async Task<bool> ExistsWithNameAsync(string name)
        => await userManager.FindByNameAsync(name) is not null;

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        var sanitized = phoneNumber.Replace(" ", "").Replace("-", "");
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == sanitized) is User u && u.Id != exceptId;
    }


    // =====================================================================
    //  GET ENRICHED PROFILE (For BFF Authentication Flow)
    // =====================================================================
    public async Task<EnrichedUserProfileDto?> GetEnrichedProfileAsync(string userId, CancellationToken ct)
    {
        try
        {
            // Single query with all related data for optimal performance
            var user = await userManager.Users
                .Include(u => u.TutorProfile)
                .Include(u => u.StudentProfile)
                .Include(u => u.UserPreference) 
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                return null;
            }

            // Get user roles
            var roles = await userManager.GetRolesAsync(user);

            // Build enriched profile
            return new EnrichedUserProfileDto
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                ProfileImageUrl = user.ProfileImageUrl?.ToString(),
                Roles = roles.Select(r => r.ToLowerInvariant()).ToArray(),
                ActiveProfile = user.UserPreference?.ActiveProfile ?? "student",
                HasAdminAccess = roles.Contains(SeedDefaults.Roles.Admin),

                TutorProfile = user.TutorProfile != null
                    ? new TutorProfileSummaryDto
                    {
                        IsActive = user.TutorProfile.IsActive,
                        IsVerified = user.TutorProfile.IsVerified,
                        IsComplete = user.TutorProfile.IsComplete,
                        ShowReminder = user.TutorProfile.ShowTutorProfileReminder
                    }
                    : null,

                StudentProfile = user.StudentProfile != null
                    ? new StudentProfileSummaryDto
                    {
                        CanBook = user.StudentProfile.CanBook,
                        SubscriptionStatus = user.StudentProfile.SubscriptionStatus.ToString(),
                        IsComplete = user.StudentProfile.IsComplete,
                        ShowReminder = user.StudentProfile.ShowStudentProfileReminder
                    }
                    : null
            };
        }
        catch (Exception ex)
        {
            // Log but don't throw - let the controller handle the null response
            // You can add logging here if you have a logger instance
            return null;
        }
    }


    // =====================================================================
    //  GET / LIST / COUNT
    // =====================================================================

    public async Task<UserDetailDto> GetAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .Include(u => u.Country)
            .Include(u => u.TutorProfile)
            .Include(u => u.StudentProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new AvanciraNotFoundException("User not found.");

        return user.Adapt<UserDetailDto>();
    }

    public async Task<UserDetailDto?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Include(u => u.Country)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        return user?.Adapt<UserDetailDto>();
    }

    public async Task<string?> GetLinkedUserIdAsync(string provider, string providerKey)
    {
        var login = await db.UserLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LoginProvider == provider && l.ProviderKey == providerKey);
        return login?.UserId;
    }

    public Task<int> GetCountAsync(CancellationToken ct)
        => userManager.Users.AsNoTracking().CountAsync(ct);

    public async Task<List<UserDetailDto>> GetListAsync(CancellationToken ct)
        => (await userManager.Users.AsNoTracking().ToListAsync(ct)).Adapt<List<UserDetailDto>>();

    // =====================================================================
    //  LINK EXTERNAL LOGIN
    // =====================================================================

    public async Task LinkExternalLoginAsync(string userId, string provider, string providerKey)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AvanciraNotFoundException("User not found.");

        var existing = await userManager.GetLoginsAsync(user);
        if (existing.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey))
            return; // already linked

        var info = new UserLoginInfo(provider, providerKey, provider);
        var result = await userManager.AddLoginAsync(user, info);

        if (!result.Succeeded)
            throw new AvanciraException("Failed to link external login.", result.Errors.Select(e => e.Description));
    }

    // =====================================================================
    //  REGISTER (STANDARD)
    // =====================================================================

    public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserDto request, string origin, CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new AvanciraException("Email already in use.");

        var normalizedCountry = await NormalizeAndValidateCountryAsync(request.CountryCode, ct);

        var user = new User
        {
            Email = request.Email,
            UserName= request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            TimeZoneId = request.TimeZoneId,
            CountryCode = normalizedCountry,
            EmailConfirmed = false,
            IsActive = false
        };

        var strategy = db.Database.CreateExecutionStrategy();
        RegisterUserResponseDto response = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new AvanciraException("Registration failed", result.Errors.Select(e => e.Description));

            var defaultProfile = request.RegisterAsTutor ? "tutor" : "student";

            if (request.RegisterAsTutor)
            {
                await userManager.AddToRoleAsync(user, SeedDefaults.Roles.Tutor);
                db.TutorProfiles.Add(TutorProfile.Create(user.Id));
            }
            else
            {
                await userManager.AddToRoleAsync(user, SeedDefaults.Roles.Student);
                db.StudentProfiles.Add(StudentProfile.Create(user.Id));
            }

            db.UserPreferences.Add(UserPreference.Create(user.Id, defaultProfile));

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            string link = await GetEmailVerificationUriAsync(user, origin);
            var evt = new ConfirmEmailEvent
            {
                UserId = user.Id,
                Email = user.Email!,
                ConfirmationLink = link
            };
            await notificationService.NotifyAsync(NotificationEvent.ConfirmEmail, evt);

            response = new RegisterUserResponseDto(user.Id);
        });

        return response;
    }

    // =====================================================================
    //  REGISTER (EXTERNAL / SOCIAL)
    // =====================================================================

    public async Task<RegisterUserResponseDto> RegisterExternalAsync(SocialRegisterDto dto, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            throw new AvanciraException("Email already registered. Please sign in normally.");

        var normalizedCountry = await NormalizeAndValidateCountryAsync(dto.CountryCode, ct);

        var user = new User
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender,
            TimeZoneId = dto.TimeZoneId,
            CountryCode = normalizedCountry,
            EmailConfirmed = true,
            IsActive = true
        };

        user.UserName = dto.Email;

        var strategy = db.Database.CreateExecutionStrategy();

        RegisterUserResponseDto response = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var result = await userManager.CreateAsync(user);

            if (!result.Succeeded)
                throw new AvanciraException("Social registration failed", result.Errors.Select(e => e.Description));
         
            var defaultProfile = dto.RegisterAsTutor ? "tutor" : "student";

            if (dto.RegisterAsTutor)
            {
                await userManager.AddToRoleAsync(user, SeedDefaults.Roles.Tutor);
                db.TutorProfiles.Add(TutorProfile.Create(user.Id));
            }
            else
            {
                await userManager.AddToRoleAsync(user, SeedDefaults.Roles.Student);
                db.StudentProfiles.Add(StudentProfile.Create(user.Id));
            }

                     db.UserPreferences.Add(UserPreference.Create(user.Id, defaultProfile));

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            response = new RegisterUserResponseDto(user.Id);
        });

        return response;
    }

    // =====================================================================
    //  TOGGLE STATUS / UPDATE / DELETE (unchanged)
    // =====================================================================

    public async Task ToggleStatusAsync(ToggleUserStatusDto request, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new AvanciraNotFoundException("User not found.");

        if (await userManager.IsInRoleAsync(user, SeedDefaults.Roles.Admin))
            throw new AvanciraException("Administrators cannot be deactivated.");

        user.IsActive = request.ActivateUser;
        await userManager.UpdateAsync(user);
    }


    // =====================================================================
    //  EMAIL / PHONE CONFIRMATION
    // =====================================================================

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

    // =====================================================================
    //  PRINCIPAL HANDLING (USED FOR EXTERNAL LOGIN PIPELINES)
    // =====================================================================

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }
    
    // =====================================================================
    //  USER PROFILE UPDATE
    // =====================================================================

    public async Task UpdateAsync(UpdateUserDto request, string userId)
    {
        var user = await userManager.Users
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);

        _ = user ?? throw new AvanciraNotFoundException("User not found.");

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

        // Update basic info
        if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.TimeZoneId)) user.TimeZoneId = request.TimeZoneId;

        if (!string.IsNullOrEmpty(request.DateOfBirth) &&
            DateOnly.TryParse(request.DateOfBirth, out var dob))
        {
            user.DateOfBirth = dob;
        }

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
            user.Address.Street = request.AddressStreetAddress;
        if (!string.IsNullOrEmpty(request.AddressCity))
            user.Address.City = request.AddressCity;
        if (!string.IsNullOrEmpty(request.AddressState))
            user.Address.State = request.AddressState;
        if (!string.IsNullOrEmpty(request.AddressPostalCode))
            user.Address.PostalCode = request.AddressPostalCode;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new AvanciraException("Update profile failed", result.Errors.Select(e => e.Description));
        }
    }

    // =====================================================================
    //  DELETE USER (SOFT DELETE)
    // =====================================================================

    public async Task DeleteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AvanciraNotFoundException("User not found.");

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new AvanciraException("Delete profile failed", errors);
        }
    }

    // =====================================================================
    //  ROLE ASSIGNMENT
    // =====================================================================

    public async Task<string> AssignRolesAsync(string userId, AssignUserRoleDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new AvanciraNotFoundException("User not found.");

        // Ensure we keep minimum number of admins
        if (await userManager.IsInRoleAsync(user, SeedDefaults.Roles.Admin) &&
            request.UserRoles.Exists(a => !a.Enabled && a.RoleName == SeedDefaults.Roles.Admin))
        {
            int adminCount = (await userManager.GetUsersInRoleAsync(SeedDefaults.Roles.Admin)).Count;
            if (adminCount <= 2)
            {
                throw new AvanciraException("System should have at least 2 admins.");
            }
        }

        foreach (var role in request.UserRoles)
        {
            if (await roleManager.FindByNameAsync(role.RoleName!) is not null)
            {
                if (role.Enabled)
                {
                    if (!await userManager.IsInRoleAsync(user, role.RoleName!))
                    {
                        await userManager.AddToRoleAsync(user, role.RoleName!);
                    }
                }
                else
                {
                    await userManager.RemoveFromRoleAsync(user, role.RoleName!);
                }
            }
        }

        return "User roles updated successfully.";
    }

    // =====================================================================
    //  GET USER ROLES
    // =====================================================================

    public async Task<List<UserRoleDetailDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AvanciraNotFoundException("User not found.");

        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken)
            ?? throw new AvanciraNotFoundException("Roles not found.");

        var userRoles = new List<UserRoleDetailDto>();
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


    // =====================================================================
    //  PRIVATE HELPERS
    // =====================================================================

    private async Task<string> GetEmailVerificationUriAsync(User user, string origin)
    {
        var sanitizedOrigin = linkBuilder.ValidateOrigin(origin, "Email verification unavailable.");
        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri($"{sanitizedOrigin}/{route}");
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), "userId", user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, "code", code);
        return verificationUri;
    }

    private async Task<string> NormalizeAndValidateCountryAsync(string? countryCode, CancellationToken ct)
    {
        var normalized = string.IsNullOrWhiteSpace(countryCode)
            ? "AU"
            : countryCode.Trim().ToUpperInvariant();

        var exists = await db.Countries.AnyAsync(c => c.Id == normalized, ct);
        if (!exists)
            throw new AvanciraNotFoundException($"Country '{countryCode}' not found.");

        return normalized;
    }

}
