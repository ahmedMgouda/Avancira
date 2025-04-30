using Avancira.Domain.Catalog.Enums;

public class CardDto
{
    public int Id { get; set; }
    public string Last4 { get; set; }
    public long ExpMonth { get; set; }
    public long ExpYear { get; set; }
    public string Type { get; set; }
    public UserCardType Purpose { get; set; }

    public CardDto()
    {
        Last4 = string.Empty;
        Type = string.Empty;
    }
}