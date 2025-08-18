namespace Avancira.Application.Common;

public sealed record ClientInfo(
    string IpAddress,
    string UserAgent,
    string OperatingSystem,
    string? Country,
    string? City
);
