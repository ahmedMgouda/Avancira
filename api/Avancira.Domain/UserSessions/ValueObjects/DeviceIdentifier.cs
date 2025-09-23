using Avancira.Domain.Common.Exceptions;
using System.Text.RegularExpressions;

namespace Avancira.Domain.UserSessions.ValueObjects
{

    public sealed record DeviceIdentifier
    {
        private static readonly Regex ValidPattern = new(@"^[a-zA-Z0-9\-_.]+$", RegexOptions.Compiled);
        private DeviceIdentifier(string value) => Value = value;
        public string Value { get; }

        public static DeviceIdentifier Create(string? value, int minLength = 8, int maxLength = 64)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new AvanciraValidationException("Device identifier cannot be empty");

            var trimmed = value.Trim();

            if (trimmed.Length < minLength)
                throw new AvanciraValidationException($"Device identifier must be at least {minLength} characters");

            if (trimmed.Length > maxLength)
                trimmed = trimmed[..maxLength];

            if (!ValidPattern.IsMatch(trimmed))
                throw new AvanciraValidationException("Device identifier contains invalid characters");

            return new DeviceIdentifier(trimmed);
        }

        public static DeviceIdentifier Generate(string prefix = "gen")
        {
            var id = $"{prefix}-{Guid.NewGuid():N}";
            return new DeviceIdentifier(id[..Math.Min(id.Length, 64)]);
        }

        public static implicit operator string(DeviceIdentifier deviceId) => deviceId.Value;
    }

}
