namespace Avancira.Application.Mail;

public class EnhancedEmailOptions
{
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class GraphApiOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}

public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
