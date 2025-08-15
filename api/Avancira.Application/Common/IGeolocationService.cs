using System.Threading.Tasks;

namespace Avancira.Application.Catalog
{
    public interface IGeolocationService
    {
        // Read
        Task<(string? Country, string? City)> GetLocationFromIpAsync(string ipAddress);
    }
}
