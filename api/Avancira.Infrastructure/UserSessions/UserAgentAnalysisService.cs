using Avancira.Application.UserSessions;
using Avancira.Domain.UserSessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAParser;

namespace Avancira.Infrastructure.UserSessions
{
    public sealed class UserAgentAnalysisService : IUserAgentAnalysisService
    {
        private readonly Parser _parser = Parser.GetDefault();

        public DeviceInformation AnalyzeUserAgent(UserAgentString userAgent)
        {
            if (userAgent.IsUnknown)
                return DeviceInformation.Unknown;

            var clientInfo = _parser.Parse(userAgent.Value);

            var deviceName = string.IsNullOrWhiteSpace(clientInfo.Device.Family)
                ? "Unknown Device"
                : clientInfo.Device.Family;

            var os = string.IsNullOrWhiteSpace(clientInfo.OS.Family)
                ? "Unknown OS"
                : clientInfo.OS.ToString();

            var browser = string.IsNullOrWhiteSpace(clientInfo.UA.Family)
                ? "Unknown Browser"
                : clientInfo.UA.ToString();

            var category = clientInfo.Device.Family.ToLower() switch
            {
                "iphone" or "android" => DeviceCategory.Mobile,
                "ipad" or "tablet" => DeviceCategory.Tablet,
                _ => DeviceCategory.Desktop
            };

            return DeviceInformation.Create(deviceName, os, browser, category);
        }
    }
}
