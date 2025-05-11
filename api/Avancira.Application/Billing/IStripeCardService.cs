using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Billing
{
    public interface IStripeCardService
    {
        // Create
        Task<bool> AddUserCardAsync(string userId, SaveCardDto request);

        // Read
        Task<IEnumerable<CardDto>> GetUserCardsAsync(string userId);

        // Delete
        Task<bool> RemoveUserCardAsync(string userId, int cardId);
    }
}
