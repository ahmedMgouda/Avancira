using Avancira.Domain.Common;
using Avancira.Domain.Common.Contracts;

namespace Avancira.Domain.Geography;

public class Country : BaseEntity<string>, IAggregateRoot
{
    private Country()
    {
    }

    private Country(
        string code,
        string name,
        string? currencyCode,
        string? dialingCode,
        bool isActive)
    {
        Id = code.ToUpperInvariant();
        Name = name;
        CurrencyCode = currencyCode;
        DialingCode = dialingCode;
        IsActive = isActive;
    }

    public string Code => Id;
    public string Name { get; private set; } = string.Empty;
    public string? CurrencyCode { get; private set; }
    public string? DialingCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Country Create(
        string code,
        string name,
        string? currencyCode,
        string? dialingCode,
        bool isActive = true) =>
        new(code, name, currencyCode, dialingCode, isActive);

    public void Update(
        string name,
        string? currencyCode,
        string? dialingCode,
        bool isActive)
    {
        Name = name;
        CurrencyCode = currencyCode;
        DialingCode = dialingCode;
        IsActive = isActive;
    }
}
