using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Billing
{
    public interface IStripeAccountService
    {
        // Create
        Task<string> ConnectStripeAccountAsync(string userId);
    }
}
