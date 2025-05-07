using Avancira.Application.Auth.Jwt;
using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class UserService : IUserService
    {
        private readonly AvanciraDbContext _dbContext;
        public UserService(
               AvanciraDbContext dbContext
           )
        {
            _dbContext = dbContext;
        }

        public Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<(bool IsSuccess, string Error)> ConfirmEmailAsync(string userId, string token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAccountAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetCompensationPercentageAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserDiplomaStatus> GetDiplomaStatusAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<object>> GetLandingPageUsersAsync()
        {
            var targetCities = new List<string> { "Sydney", "Brisbane", "Perth" };

            // Fetch actual mentor counts from the database
            //var jobLocations = await _dbContext.Users
            //    .Where(u => u.Address != null && targetCities.Contains(u.Address.City))
            //    .GroupBy(u => u.Address.City)
            //    .Select(group => new
            //    {
            //        City = group.Key,
            //        Mentors = group.Count()
            //    })
            //    .ToDictionaryAsync(x => x.City, x => x.Mentors);

            // Define default job locations and override counts if data exists
            var predefinedLocations = targetCities.Select(city => (object)new
            {
                Img = $"assets/img/city/city_{city.ToLower()}.jpg",
                City = city,
                Country = "Australia",
                Mentors =0 /*jobLocations.GetValueOrDefault(city, 0)*/
            }).ToList();

            return predefinedLocations;
        }

        public Task<string?> GetPaymentGatewayAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserPaymentSchedule?> GetPaymentScheduleAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserDto?> GetUserAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserDto?> GetUserByReferralTokenAsync(string recommendationToken)
        {
            throw new NotImplementedException();
        }

        public Task<(string Token, List<string> Roles)?> LoginUserAsync(LoginViewModel model)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyPaymentScheduleAsync(string userId, UserPaymentSchedule paymentSchedule)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ModifyUserAsync(string userId, UserDto updatedUser)
        {
            throw new NotImplementedException();
        }

        public Task<(bool IsSuccess, string Error)> RegisterUserAsync(RegisterViewModel model, string country)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetPassword(string email, string token, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendPasswordResetEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<SocialLoginResult> SocialLoginAsync(string provider, string token, string? country = null, string? referralToken = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SubmitDiplomaAsync(string userId, IFormFile diplomaFile, string? diplomaDescription = null)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCompensationPercentageAsync(string userId, int newPercentage)
        {
            throw new NotImplementedException();
        }
    }
}
