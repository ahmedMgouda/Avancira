using Avancira.Domain.Common.Exceptions;
using System.Net;

namespace Avancira.Domain.UserSessions.ValueObjects
{
    public sealed record IpAddress
    {
        private IpAddress(string value) => Value = value;
        public string Value { get; }

        public static IpAddress Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new AvanciraValidationException("IP address cannot be empty");

            var trimmed = value.Trim();
            if (!IPAddress.TryParse(trimmed, out _))
                throw new AvanciraValidationException($"Invalid IP address format: {trimmed}");

            return new IpAddress(trimmed);
        }

        public bool IsPrivate()
        {
            if (!IPAddress.TryParse(Value, out var ip))
                return false;

            if (IPAddress.IsLoopback(ip)) return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return bytes[0] == 10 ||
                       bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                       bytes[0] == 192 && bytes[1] == 168 ||
                       bytes[0] == 169 && bytes[1] == 254;
            }

            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
        }

        public static implicit operator string(IpAddress ipAddress) => ipAddress.Value;
    }
}
