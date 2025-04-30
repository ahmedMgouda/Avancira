using Avancira.Domain.Catalog.Enums;

public class SaveCardDto
{
    public string StripeToken { get; set; }
    public UserCardType Purpose { get; set; }

    public SaveCardDto()
    {
        StripeToken = string.Empty;
    }
}