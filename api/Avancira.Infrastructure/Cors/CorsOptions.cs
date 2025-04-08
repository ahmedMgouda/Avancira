using System.Collections.ObjectModel;

namespace Avancira.Infrastructure.Cors;
public class CorsOptions
{
    public CorsOptions()
    {
        AllowedOrigins = [];
    }

    public Collection<string> AllowedOrigins { get; }
}
