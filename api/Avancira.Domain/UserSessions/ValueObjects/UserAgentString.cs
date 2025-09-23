namespace Avancira.Domain.UserSessions.ValueObjects
{
    public sealed record UserAgentString
    {
        private const string UnknownValue = "Unknown";
        private UserAgentString(string value) => Value = value;
        public string Value { get; }
        public bool IsUnknown => Value == UnknownValue;

        public static UserAgentString Create(string? value) =>
            new(string.IsNullOrWhiteSpace(value) ? UnknownValue : value.Trim());

        public static implicit operator string(UserAgentString userAgent) => userAgent.Value;
    }
}
