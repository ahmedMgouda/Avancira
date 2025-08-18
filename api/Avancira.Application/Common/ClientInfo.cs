namespace Avancira.Application.Common;

public class ClientInfo
{
    public string DeviceId { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public string OperatingSystem { get; set; } = default!;
    public string? Country { get; set; }
    public string? City { get; set; }
}
