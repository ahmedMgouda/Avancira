using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Net;

namespace Avancira.Infrastructure.Common
{
    public class NetworkContextHandler
    {
        private readonly ILogger<NetworkContextHandler> _logger;
        private readonly DeviceIdSettings _deviceIdOptions;

        private static readonly string[] ProxyHeaders =
        {
        "X-Real-IP", "X-Forwarded-For", "CF-Connecting-IP",
        "X-Client-IP", "X-Cluster-Client-IP", "Forwarded"
    };

        private static readonly string[] DeviceIdSources =
        {
        "X-Device-ID", "X-Client-ID", "Device-ID"
    };

        public NetworkContextHandler(ILogger<NetworkContextHandler> logger, IOptions<SessionMetadataOptions> options)
        {
            _logger = logger;
            _deviceIdOptions = options.Value.DeviceId;
        }

        public string ExtractIpAddress(HttpContext context)
        {
            try
            {
                // Try proxy headers first
                foreach (var header in ProxyHeaders)
                {
                    if (context.Request.Headers.TryGetValue(header, out var headerValue))
                    {
                        var ip = ParseIpFromHeader(headerValue.ToString());
                        if (!string.IsNullOrWhiteSpace(ip))
                        {
                            _logger.LogDebug("IP extracted from {Header}: {IP}", header, ip);
                            return ip;
                        }
                    }
                }

                // Fallback to connection IP
                var directIp = context.Connection.RemoteIpAddress?.ToString();
                return NormalizeIpAddress(directIp) ?? "127.0.0.1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting IP address");
                return "127.0.0.1";
            }
        }

        public string ExtractAndValidateDeviceId(HttpContext context)
        {
            var deviceId = TryExtractDeviceId(context);
            return ValidateDeviceId(deviceId);
        }

        private string? TryExtractDeviceId(HttpContext context)
        {
            // Try headers
            foreach (var header in DeviceIdSources)
            {
                if (context.Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.ToString().Trim();
            }

            // Try query parameters
            foreach (var param in new[] { "deviceId", "device_id", "clientId" })
            {
                if (context.Request.Query.TryGetValue(param, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.ToString().Trim();
            }

            // Try cookies
            foreach (var cookie in new[] { "device_id", "client_id" })
            {
                if (context.Request.Cookies.TryGetValue(cookie, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private string ValidateDeviceId(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return GenerateFallbackDeviceId("empty");

            if (deviceId.Length < _deviceIdOptions.MinLength)
                return GenerateFallbackDeviceId("too-short");

            if (deviceId.Length > _deviceIdOptions.MaxLength)
                deviceId = deviceId[.._deviceIdOptions.MaxLength];

            // Sanitize: keep only alphanumeric, hyphens, underscores, dots
            var sanitized = Regex.Replace(deviceId, @"[^a-zA-Z0-9\-_.]", "");

            return string.IsNullOrWhiteSpace(sanitized) ? GenerateFallbackDeviceId("invalid") : sanitized;
        }

        private string GenerateFallbackDeviceId(string reason)
        {
            if (!_deviceIdOptions.GenerateFallback)
                return "unknown";

            var fallback = $"fb-{reason}-{Guid.NewGuid():N}"[.._deviceIdOptions.MaxLength];
            _logger.LogWarning("Generated fallback device ID: {DeviceId} (reason: {Reason})", fallback, reason);
            return fallback;
        }

        private string? ParseIpFromHeader(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                return null;

            // Handle comma-separated IPs (X-Forwarded-For)
            var ips = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(ip => ip.Trim())
                                .Where(ip => !string.IsNullOrWhiteSpace(ip));

            foreach (var ip in ips)
            {
                var cleanIp = ExtractIpFromForwardedFormat(ip);
                if (IsValidPublicIp(cleanIp))
                    return cleanIp;
            }

            // Return first valid IP (even if private)
            return ips.Select(ExtractIpFromForwardedFormat).FirstOrDefault(IsValidIp);
        }

        private string ExtractIpFromForwardedFormat(string ipString)
        {
            // Handle "Forwarded" header: for=192.168.1.1:8080
            if (ipString.StartsWith("for=", StringComparison.OrdinalIgnoreCase))
            {
                var forValue = ipString[4..].Trim('"');
                if (forValue.StartsWith('[') && forValue.Contains(']'))
                    return forValue[1..forValue.IndexOf(']')]; // IPv6

                var colonIndex = forValue.LastIndexOf(':');
                return colonIndex > 0 ? forValue[..colonIndex] : forValue;
            }

            return ipString;
        }

        private string? NormalizeIpAddress(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return null;

            // IPv6 localhost to IPv4
            if (ipAddress == "::1")
                return "127.0.0.1";

            // IPv4-mapped IPv6
            if (ipAddress.StartsWith("::ffff:"))
            {
                var ipv4Part = ipAddress[7..];
                return IsValidIp(ipv4Part) ? ipv4Part : null;
            }

            return IsValidIp(ipAddress) ? ipAddress : null;
        }

        private static bool IsValidIp(string? ip) =>
            !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out _);

        private static bool IsValidPublicIp(string? ip)
        {
            if (!IsValidIp(ip) || !IPAddress.TryParse(ip, out var address))
                return false;

            if (IPAddress.IsLoopback(address))
                return false;

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return !(bytes[0] == 10 ||
                        (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                        (bytes[0] == 192 && bytes[1] == 168) ||
                        (bytes[0] == 169 && bytes[1] == 254));
            }

            return true; // Assume IPv6 is public for simplicity
        }
    }

}
