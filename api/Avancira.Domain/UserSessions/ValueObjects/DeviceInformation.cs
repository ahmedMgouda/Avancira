namespace Avancira.Domain.UserSessions.ValueObjects
{
    public sealed record DeviceInformation
    {
        private DeviceInformation(string name, string os, string browser, DeviceCategory category)
        {
            Name = name;
            OperatingSystem = os;
            BrowserName = browser;
            Category = category;
        }

        public string Name { get; }
        public string OperatingSystem { get; }
        public string BrowserName { get; }
        public DeviceCategory Category { get; }

        public static DeviceInformation Create(string name, string os, string browser, DeviceCategory category) =>
            new(name, os, browser, category);

        public static DeviceInformation Unknown =>
            new("Unknown Device", "Unknown OS", "Unknown Browser", DeviceCategory.Desktop);
    }

    public enum DeviceCategory
    {
        Desktop = 1,
        Mobile = 2,
        Tablet = 3,
        SmartTV = 4,
        Console = 5,
        Bot = 6
    }
}
