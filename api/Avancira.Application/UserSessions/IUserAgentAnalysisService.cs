using Avancira.Domain.UserSessions.ValueObjects;

namespace Avancira.Application.UserSessions
{
    /// <summary>
    /// Analyzes raw user agent strings and extracts structured device information.
    /// </summary>
    public interface IUserAgentAnalysisService
    {
        /// <summary>
        /// Parses a user agent string into <see cref="DeviceInformation"/>.
        /// </summary>
        /// <param name="userAgent">The raw user agent string (wrapped in a VO).</param>
        /// <returns>A <see cref="DeviceInformation"/> containing parsed device info.</returns>
        DeviceInformation AnalyzeUserAgent(UserAgentString userAgent);
    }
}
