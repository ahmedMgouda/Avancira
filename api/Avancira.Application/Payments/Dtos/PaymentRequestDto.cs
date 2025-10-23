using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Payments.Dtos
{
    public class PaymentRequestDto
    {
        public string Gateway { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AUD";
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }

        public PaymentRequestDto()
        {
            Gateway = string.Empty;
            ReturnUrl = string.Empty;
            CancelUrl = string.Empty;
        }
    }
}
