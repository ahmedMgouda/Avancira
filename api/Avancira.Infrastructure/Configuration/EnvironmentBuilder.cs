namespace Avancira.Infrastructure.Configuration;

public static class EnvironmentBuilder
{
    private const string Prefix = "Avancira__";

    public static AppEnvironment BuildFromEnvironment()
    {
        var p = ServiceRegistry.GetService("postgres");

        return new AppEnvironment(
            Get($"{Prefix}Database__Host", "localhost"),
            Get($"{Prefix}Database__Port", p.HttpPort.ToString()),
            Get($"{Prefix}Database__Name", "avancira"),
            Get($"{Prefix}Database__User", "Avancira"),
            Get($"{Prefix}Database__Password", "Avancira@2025"),
            Get($"{Prefix}Auth__Issuer", ServiceRegistry.Endpoints.Auth),
            Get($"{Prefix}Jwt__Audience", "AvanciraAudience"),
            Get($"{Prefix}Jwt__Issuer", "AvanciraIssuer"),
            Get($"{Prefix}Jwt__Key", "AvanciraSecretKey"),
            Get($"{Prefix}App__BaseUrl", ServiceRegistry.Endpoints.Bff),
            Get($"{Prefix}App__FrontEndUrl", ServiceRegistry.Endpoints.Frontend),
            Get($"{Prefix}App__Name", "Avancira"),
            Get($"{Prefix}App__SupportEmail", "support@avancira.com"),
            Get($"{Prefix}App__SupportPhone", "+1-800-000-0000"),
            Get($"{Prefix}ExternalServices__Google__ClientId"),
            Get($"{Prefix}ExternalServices__Google__ClientSecret"),
            Get($"{Prefix}ExternalServices__Facebook__AppId"),
            Get($"{Prefix}ExternalServices__Facebook__AppSecret"),
            Get($"{Prefix}Notifications__Smtp__Host"),
            Get($"{Prefix}Notifications__Smtp__Port"),
            Get($"{Prefix}Notifications__Email__From"),
            Get($"{Prefix}Notifications__Email__FromName"),
            Get($"{Prefix}Notifications__SendGrid__ApiKey"),
            Get($"{Prefix}Notifications__Twilio__AccountSid"),
            Get($"{Prefix}Notifications__Twilio__AuthToken"),
            Get($"{Prefix}Notifications__Twilio__PhoneNumber"),
            Get($"{Prefix}Notifications__GraphApi__ClientId"),
            Get($"{Prefix}Notifications__GraphApi__ClientSecret"),
            Get($"{Prefix}Notifications__GraphApi__TenantId"),
            Get($"{Prefix}Payments__Stripe__ApiKey"),
            Get($"{Prefix}Payments__Stripe__PublishableKey"),
            Get($"{Prefix}Payments__Stripe__SecretKey"),
            Get($"{Prefix}Payments__Paypal__ClientId"),
            Get($"{Prefix}Payments__Paypal__ClientSecret"),
            Get($"{Prefix}Payments__Paypal__Environment"),
            Get($"{Prefix}Jitsi__AppId"),
            Get($"{Prefix}Jitsi__AppSecret"),
            Get($"{Prefix}Jitsi__Domain")
        );
    }

    private static string Get(string key, string fallback = "") =>
        Environment.GetEnvironmentVariable(key) ?? fallback;
}
