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
                .Map(dest => dest.FullName, src => src.FullName)
                .Map(dest => dest.Email, src => src.Email)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.EmailConfirmed, src => src.EmailConfirmed)
                .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                .Map(dest => dest.CountryCode, src => src.CountryCode)
                .Map(dest => dest.CountryName, src => src.Country.Name)
                .Map(dest => dest.DialingCode, src => src.Country.DialingCode)
                .Map(dest => dest.Gender, src => src.Gender)
                .Map(dest => dest.ProfileImageUrl, src => src.ProfileImageUrl)
                .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                .Map(dest => dest.TimeZoneId, src => src.TimeZoneId)
                .Map(dest => dest.Address, src => src.Address == null ? null : new AddressDto
                {
                    Street = src.Address.Street,
                    City = src.Address.City,
                    State = src.Address.State,
                    PostalCode = src.Address.PostalCode
                })
                .Map(dest => dest.CreatedOnUtc, src => src.CreatedOnUtc)
                .Map(dest => dest.LastModifiedOnUtc, src => src.LastModifiedOnUtc);
        }
    }
}
