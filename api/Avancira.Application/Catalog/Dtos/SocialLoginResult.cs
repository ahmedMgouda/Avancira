using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Avancira.Application.Catalog.Dtos
{
    public class SocialLoginResult
    {
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        [JsonPropertyName("isRegistered")]
        public bool IsRegistered { get; set; }
    }
}
