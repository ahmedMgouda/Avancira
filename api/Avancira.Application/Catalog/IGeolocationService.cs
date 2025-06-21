using System.Threading.Tasks;

namespace Avancira.Application.Catalog
{
    public interface IGeolocationService
    {
        // Read
        Task<string?> GetCountryFromIpAsync(string ipAddress);
    }
}
