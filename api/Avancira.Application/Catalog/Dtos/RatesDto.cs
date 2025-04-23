using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class RatesDto
    {
        public decimal Hourly { get; set; }
        public decimal FiveHours { get; set; }
        public decimal TenHours { get; set; }
    }
}
