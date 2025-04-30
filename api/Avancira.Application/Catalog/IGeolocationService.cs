using System.Threading.Tasks;

namespace Backend.Interfaces
{
    public interface IGeolocationService
    {
        // Read
        Task<string?> GetCountryFromIpAsync(string ipAddress);
    }
}

