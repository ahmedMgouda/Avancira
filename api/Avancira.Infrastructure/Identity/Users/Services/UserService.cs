using Avancira.Application.Caching;
using Avancira.Application.Events;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Application.Jobs;
using Avancira.Application.Storage;
using Avancira.Application.Storage.File;
using Avancira.Application.Storage.File.Dtos;
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
        var user = await userManager.Users.AsNoTracking()
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

        var normalizedCountry = request.CountryCode.ToUpperInvariant();
        if (!await db.Countries.AnyAsync(c => c.Code == normalizedCountry, ct))
            throw new AvanciraNotFoundException($"Country '{request.CountryCode}' not found.");

        var user = new User
        {
            Email = request.Email,
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

    public async Task<User> RegisterExternalAsync(SocialRegisterDto dto, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            throw new AvanciraException("Email already registered. Please sign in normally.");

        var normalizedCountry = dto.CountryCode?.ToUpperInvariant() ?? "US";

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

        var strategy = db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new AvanciraException("Social registration failed", result.Errors.Select(e => e.Description));

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

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return user;
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
}
