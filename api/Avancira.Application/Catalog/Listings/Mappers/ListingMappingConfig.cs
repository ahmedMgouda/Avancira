using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Catalog.Listings.Dtos;
using Avancira.Domain.Catalog;
using Avancira.Domain.Catalog.Enums;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Listings.Mappers
{
    public class ListingMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Listing, ListingDto>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.IsVisible, src => src.IsVisible)
                .Map(dest => dest.ContactedCount, src => 0)
                .Map(dest => dest.Reviews, src => 0)
                .Map(dest => dest.Title, src => src.Name)
                .Map(dest => dest.ListingImagePath, src => string.Empty)
                .Map(dest => dest.AboutLesson, src => src.Description)
                .Map(dest => dest.AboutYou, src => string.Empty)
                .Map(dest => dest.Rate, src => $"{src.HourlyRate}/h")
                .Map(dest => dest.Rates, src => new RatesDto
                {
                    Hourly = src.HourlyRate,
                    FiveHours = src.HourlyRate * 5,
                    TenHours = src.HourlyRate * 10
                })
                .Map(dest => dest.SocialPlatforms, src => new List<string> { "Messenger", "Linkedin", "Facebook", "Email" });

            // --- Add opposite mapping ---
            config.NewConfig<ListingRequestDto, Listing>()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.HourlyRate, src => src.HourlyRate)
                .Map(dest => dest.LocationType, src => ListingLocationType.Webcam);
        }
    }

}
