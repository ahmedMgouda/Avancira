using Avancira.Application.UserSessions;
using Avancira.Domain.UserSessions.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using UAParser;

namespace Avancira.Infrastructure.UserSessions;

public sealed class UserAgentAnalysisService : IUserAgentAnalysisService
{
    private readonly ILogger<UserAgentAnalysisService> _logger;
    private readonly Parser _parser;
    private readonly Regex _chromeVersionRegex;

    // Configuration constants
    private static class Constants
    {
        public const int Windows11ChromeThreshold = 94; // Chrome 94+ indicates Windows 11
        public const int RegexTimeoutMs = 100;
        public const string UnknownDevice = "Unknown Device";
        public const string UnknownOS = "Unknown OS";
        public const string UnknownBrowser = "Unknown Browser";
        public const string Other = "Other";
    }

    private static readonly DeviceInformation Fallback =
        DeviceInformation.Create(
            Constants.UnknownDevice,
            Constants.UnknownOS,
            Constants.UnknownBrowser,
            DeviceCategory.Desktop);

    // Browser identifiers ordered by specificity (most specific first)
    private static readonly IReadOnlyList<(string Identifier, string Name)> BrowserIdentifiers = new[]
    {
        ("edg", "Microsoft Edge"),
        ("edge", "Microsoft Edge"),
        ("opr", "Opera"),
        ("opera", "Opera"),
        ("brave", "Brave"),
        ("vivaldi", "Vivaldi"),
        ("yabrowser", "Yandex Browser"),
        ("samsungbrowser", "Samsung Internet"),
        ("ucbrowser", "UC Browser"),
        ("duckduckgo", "DuckDuckGo Browser"),
        ("firefox", "Mozilla Firefox"),
        ("safari", "Safari"),
        ("chrome", "Google Chrome"), // Must be after Edge/Opera as they contain "chrome"
        ("msie", "Internet Explorer"),
        ("internetexplorer", "Internet Explorer")
    };

    private static readonly IReadOnlySet<string> LinuxDistributions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Ubuntu", "Fedora", "CentOS", "SUSE", "Debian",
        "Red Hat", "Arch", "Mint", "Elementary", "Manjaro"
    };

    private static readonly IReadOnlySet<string> MobileKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Mobile", "Phone", "Android", "iPhone"
    };

    public UserAgentAnalysisService(ILogger<UserAgentAnalysisService> logger)
    {
        _logger = logger;
        _parser = Parser.GetDefault();
        _chromeVersionRegex = CreateChromeVersionRegex();
    }

    public DeviceInformation AnalyzeUserAgent(UserAgentString userAgentString)
    {
        if (!IsValidUserAgent(userAgentString))
        {
            return DeviceInformation.Unknown;
        }

        var userAgent = userAgentString.Value;

        try
        {
            var parsed = ParseUserAgent(userAgent);

            return DeviceInformation.Create(
                DetermineDeviceName(parsed, userAgent),
                DetermineOperatingSystem(parsed, userAgent),
                DetermineBrowser(parsed, userAgent),
                DetermineCategory(parsed, userAgent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze user agent: {UserAgent}", userAgent);
            return Fallback;
        }
    }

    private static Regex CreateChromeVersionRegex()
    {
        return new Regex(
            @"Chrome/(\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(Constants.RegexTimeoutMs));
    }

    private static bool IsValidUserAgent(UserAgentString? userAgentString)
    {
        return userAgentString is not null &&
               !string.IsNullOrWhiteSpace(userAgentString.Value) &&
               !userAgentString.IsUnknown;
    }

    private ParsedUserAgent ParseUserAgent(string userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);

        try
        {
            var userAgentInfo = _parser.ParseUserAgent(userAgent);
            var osInfo = _parser.ParseOS(userAgent);
            var deviceInfo = _parser.ParseDevice(userAgent);

            return new ParsedUserAgent
            {
                DeviceFamily = deviceInfo?.Family ?? Constants.Other,
                OSFamily = osInfo?.Family ?? Constants.Other,
                OSMajor = osInfo?.Major ?? string.Empty,
                OSMinor = osInfo?.Minor ?? string.Empty,
                UAFamily = userAgentInfo?.Family ?? Constants.Other,
                UAMajor = userAgentInfo?.Major ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UAParser failed for user agent: {UserAgent}, using fallback values", userAgent);
            return ParsedUserAgent.CreateEmpty();
        }
    }

    private static string DetermineDeviceName(ParsedUserAgent parsed, string userAgent)
    {
        if (IsValidDeviceFamily(parsed.DeviceFamily))
        {
            return parsed.DeviceFamily;
        }

        return parsed.OSFamily.ToLowerInvariant() switch
        {
            var os when os.Contains("windows", StringComparison.OrdinalIgnoreCase) =>
                userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ? "Windows Tablet" : "Windows PC",
            var os when os.Contains("mac", StringComparison.OrdinalIgnoreCase) =>
                DetermineAppleDevice(userAgent),
            var os when os.Contains("linux", StringComparison.OrdinalIgnoreCase) =>
                "Linux Device",
            var os when os.Contains("android", StringComparison.OrdinalIgnoreCase) =>
                "Android Device",
            var os when os.Contains("ios", StringComparison.OrdinalIgnoreCase) =>
                DetermineIOSDevice(userAgent),
            _ => Constants.UnknownDevice
        };
    }

    private static bool IsValidDeviceFamily(string? deviceFamily)
    {
        return !string.IsNullOrWhiteSpace(deviceFamily) &&
               !string.Equals(deviceFamily, Constants.Other, StringComparison.OrdinalIgnoreCase) &&
               !deviceFamily.Contains("Generic", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineAppleDevice(string userAgent)
    {
        return userAgent switch
        {
            var ua when ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) => "iPhone",
            var ua when ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) => "iPad",
            _ => "Mac"
        };
    }

    private static string DetermineIOSDevice(string userAgent)
    {
        return userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iPad" : "iPhone";
    }

    private string DetermineOperatingSystem(ParsedUserAgent parsed, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(parsed.OSFamily) ||
            string.Equals(parsed.OSFamily, Constants.Other, StringComparison.OrdinalIgnoreCase))
        {
            return Constants.UnknownOS;
        }

        return parsed.OSFamily.ToLowerInvariant() switch
        {
            "windows" => DetermineWindowsVersion(parsed, userAgent),
            var os when os.Contains("mac", StringComparison.OrdinalIgnoreCase) =>
                DetermineMacOSVersion(parsed),
            "linux" => DetermineLinuxDistribution(userAgent),
            "android" => DetermineAndroidVersion(parsed),
            "ios" => DetermineIOSVersion(parsed),
            _ => parsed.OSFamily
        };
    }

    private string DetermineWindowsVersion(ParsedUserAgent parsed, string userAgent)
    {
        return parsed.OSMajor switch
        {
            "10" when IsWindows11(userAgent) => "Windows 11",
            "10" => "Windows 10",
            "6" => DetermineWindows6Version(parsed.OSMinor),
            "5" => parsed.OSMinor == "1" ? "Windows XP" : $"Windows NT {parsed.OSMajor}.{parsed.OSMinor}",
            null or "" => "Windows",
            _ => $"Windows NT {parsed.OSMajor}"
        };
    }

    private static string DetermineWindows6Version(string? osMinor)
    {
        return osMinor switch
        {
            "3" => "Windows 8.1",
            "2" => "Windows 8",
            "1" => "Windows 7",
            "0" => "Windows Vista",
            _ => "Windows NT 6.x"
        };
    }

    private static string DetermineMacOSVersion(ParsedUserAgent parsed)
    {
        if (string.IsNullOrEmpty(parsed.OSMajor))
        {
            return "macOS";
        }

        var version = $"macOS {parsed.OSMajor}";
        if (!string.IsNullOrEmpty(parsed.OSMinor))
        {
            version += $".{parsed.OSMinor}";
        }

        return version;
    }

    private static string DetermineLinuxDistribution(string userAgent)
    {
        return LinuxDistributions.FirstOrDefault(distro =>
            userAgent.Contains(distro, StringComparison.OrdinalIgnoreCase)) ?? "Linux";
    }

    private static string DetermineAndroidVersion(ParsedUserAgent parsed)
    {
        return string.IsNullOrEmpty(parsed.OSMajor) ? "Android" : $"Android {parsed.OSMajor}";
    }

    private static string DetermineIOSVersion(ParsedUserAgent parsed)
    {
        return string.IsNullOrEmpty(parsed.OSMajor) ? "iOS" : $"iOS {parsed.OSMajor}";
    }

    private static string DetermineBrowser(ParsedUserAgent parsed, string userAgent)
    {
        // Check browser identifiers in order of specificity
        foreach (var (identifier, browserName) in BrowserIdentifiers)
        {
            if (userAgent.Contains(identifier, StringComparison.OrdinalIgnoreCase))
            {
                return FormatBrowserName(browserName, parsed.UAMajor);
            }
        }

        // Fallback to parsed user agent family
        if (IsValidUserAgentFamily(parsed.UAFamily))
        {
            return FormatBrowserName(parsed.UAFamily, parsed.UAMajor);
        }

        return Constants.UnknownBrowser;
    }

    private static bool IsValidUserAgentFamily(string? uaFamily)
    {
        return !string.IsNullOrWhiteSpace(uaFamily) &&
               !string.Equals(uaFamily, Constants.Other, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatBrowserName(string browserName, string? majorVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(browserName);

        return string.IsNullOrWhiteSpace(majorVersion)
            ? browserName
            : $"{browserName} {majorVersion}";
    }

    private static DeviceCategory DetermineCategory(ParsedUserAgent parsed, string userAgent)
    {
        var deviceFamily = parsed.DeviceFamily.ToLowerInvariant();
        var osFamily = parsed.OSFamily.ToLowerInvariant();

        if (IsTablet(deviceFamily, userAgent))
        {
            return DeviceCategory.Tablet;
        }

        if (IsMobile(deviceFamily, osFamily, userAgent))
        {
            return DeviceCategory.Mobile;
        }

        return DeviceCategory.Desktop;
    }

    private static bool IsTablet(string deviceFamily, string userAgent)
    {
        return deviceFamily.Contains("tablet", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMobile(string deviceFamily, string osFamily, string userAgent)
    {
        return deviceFamily.Contains("mobile", StringComparison.OrdinalIgnoreCase) ||
               deviceFamily.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
               (string.Equals(osFamily, "ios", StringComparison.OrdinalIgnoreCase) &&
                userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) ||
               (string.Equals(osFamily, "android", StringComparison.OrdinalIgnoreCase) &&
                ContainsAnyMobileKeyword(userAgent));
    }

    private static bool ContainsAnyMobileKeyword(string userAgent)
    {
        return MobileKeywords.Any(keyword =>
            userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsWindows11(string userAgent)
    {
        try
        {
            var match = _chromeVersionRegex.Match(userAgent);
            return match.Success &&
                   int.TryParse(match.Groups[1].Value, out var version) &&
                   version >= Constants.Windows11ChromeThreshold;
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Chrome version regex timed out for user agent: {UserAgent}", userAgent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Chrome version from user agent: {UserAgent}", userAgent);
            return false;
        }
    }

    private sealed record ParsedUserAgent
    {
        public required string DeviceFamily { get; init; }
        public required string OSFamily { get; init; }
        public required string OSMajor { get; init; }
        public required string OSMinor { get; init; }
        public required string UAFamily { get; init; }
        public required string UAMajor { get; init; }

        public static ParsedUserAgent CreateEmpty() => new()
        {
            DeviceFamily = Constants.Other,
            OSFamily = Constants.Other,
            OSMajor = string.Empty,
            OSMinor = string.Empty,
            UAFamily = Constants.Other,
            UAMajor = string.Empty
        };
    }
}