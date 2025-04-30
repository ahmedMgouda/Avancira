using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class Meeting
    {
        public string Token { get; set; } = string.Empty;
        public string MeetingUrl { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
    }
}
