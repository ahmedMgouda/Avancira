using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Common.Extensions
{
    public static class StringExtensions
    {
        public static string ExpandEnvironmentVariables(this string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            var pattern = @"\{([^}]+)\}";
            return Regex.Replace(connectionString, pattern, match =>
            {
                var envVar = match.Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(envVar);
                return !string.IsNullOrEmpty(envValue) ? envValue : match.Value;
            });
        }
    }
}
