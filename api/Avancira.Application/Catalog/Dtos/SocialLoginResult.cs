using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class SocialLoginResult
    {
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; }
        public bool isRegistered { get; set; }
    }
}
