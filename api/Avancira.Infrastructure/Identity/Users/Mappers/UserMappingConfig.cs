using Avancira.Application.Identity.Users.Dtos;
using Avancira.Infrastructure.Identity.Users;
using Mapster;

namespace Avancira.Infrastructure.Identity.Users.Mappers
{
    public class UserMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<User, UserDetailDto>()
                .Map(dest => dest.Id, src => Guid.Parse(src.Id))
                .Map(dest => dest.UserName, src => src.UserName)
                .Map(dest => dest.FirstName, src => src.FirstName)
                .Map(dest => dest.LastName, src => src.LastName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.EmailConfirmed, src => src.EmailConfirmed)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.ImageUrl, src => src.ImageUrl)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.Bio, src => src.Bio)
                .Map(dest => dest.TimeZoneId, src => src.TimeZoneId)
                .Map(dest => dest.SkypeId, src => src.SkypeId)
                .Map(dest => dest.HangoutId, src => src.HangoutId)
                .Map(dest => dest.Address, src => src.Address)
                .Map(dest => dest.PayPalAccountId, src => src.PayPalAccountId)
                .Map(dest => dest.StripeCustomerId, src => src.StripeCustomerId)
                .Map(dest => dest.StripeConnectedAccountId, src => src.StripeConnectedAccountId);
        }
    }
}
