using Ardalis.Specification;
using Avancira.Domain.Users;

namespace Avancira.Application.UserPreferences.Specifications;

public sealed class UserPreferenceByUserSpec : Specification<UserPreference>
{
    public UserPreferenceByUserSpec(string userId)
    {
        Query.Where(preference => preference.UserId == userId);
    }
}
