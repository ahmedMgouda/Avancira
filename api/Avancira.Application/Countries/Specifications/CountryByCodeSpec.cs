using Ardalis.Specification;
using Avancira.Domain.Geography;

namespace Avancira.Application.Countries.Specifications;

public sealed class CountryByCodeSpec : Specification<Country>, ISingleResultSpecification<Country>
{
    public CountryByCodeSpec(string code)
    {
        Query
            .Where(c => c.Code == code.ToUpperInvariant());
    }
}
