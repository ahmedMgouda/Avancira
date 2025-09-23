using Avancira.Domain.Common.Exceptions;

namespace Avancira.Domain.Identity.ValueObjects
{
    public sealed record GeographicLocation
    {
        private GeographicLocation(string country, string? city)
        {
            Country = country;
            City = city;
        }

        public string Country { get; }
        public string? City { get; }

        public static GeographicLocation Create(string? country, string? city = null)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new AvanciraValidationException("Country is required");

            return new GeographicLocation(country.Trim(), city?.Trim());
        }

        public override string ToString() =>
            string.IsNullOrEmpty(City) ? Country : $"{City}, {Country}";
    }
}
