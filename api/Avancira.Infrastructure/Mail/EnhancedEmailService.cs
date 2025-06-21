using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using Avancira.Application.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Avancira.Infrastructure.Mail;

public class EnhancedEmailService : IEnhancedEmailService
{
    private readonly EnhancedEmailOptions _emailOptions;
    private readonly GraphApiOptions _graphApiOptions;
    private readonly MailOptions _smtpOptions;
    private readonly SendGridOptions _sendGridOptions;
    private readonly ILogger<EnhancedEmailService> _logger;

    public EnhancedEmailService(
        IOptions<EnhancedEmailOptions> emailOptions,
        IOptions<GraphApiOptions> graphApiOptions,
        IOptions<MailOptions> smtpOptions,
        IOptions<SendGridOptions> sendGridOptions,
        ILogger<EnhancedEmailService> logger
    )
    {
        _emailOptions = emailOptions.Value;
        _graphApiOptions = graphApiOptions.Value;
        _smtpOptions = smtpOptions.Value;
        _sendGridOptions = sendGridOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, string provider = "Smtp", CancellationToken cancellationToken = default)
    {
        try
        {
            switch (provider)
            {
                case "SendGrid":
                    await SendEmailUsingSendGridAsync(toEmail, subject, body, cancellationToken);
                    break;

                case "Smtp":
                    await SendEmailUsingSmtpAsync(toEmail, subject, body, cancellationToken);
                    break;

                case "GraphApi":
                    await SendEmailUsingGraphApiAsync(toEmail, subject, body, cancellationToken);
                    break;

                default:
                    throw new NotSupportedException($"The email provider '{provider}' is not supported.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}. Subject: {Subject}, Provider: {Provider}", toEmail, subject, provider);
            throw;
        }
    }

    private async Task SendEmailUsingSendGridAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        var client = new SendGridClient(_sendGridOptions.ApiKey);

        var from = new EmailAddress(_emailOptions.FromEmail, _emailOptions.FromName);
        var to = new EmailAddress(toEmail);
        var emailMessage = MailHelper.CreateSingleEmail(from, to, subject, body, body);

        try
        {
            var response = await client.SendEmailAsync(emailMessage, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Email successfully sent to {Recipient}. Subject: {Subject}, Status Code: {StatusCode}",
                                        toEmail, subject, response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Email to {Recipient} returned with status code {StatusCode}. Subject: {Subject}",
                                    toEmail, response.StatusCode, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}. Subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    private async Task SendEmailUsingSmtpAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            using var smtpClient = new System.Net.Mail.SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                Credentials = new NetworkCredential(_smtpOptions.UserName, _smtpOptions.Password),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("SMTP Email sent successfully to {Recipient}. Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP Error sending email to {Recipient}. Subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    private async Task SendEmailUsingGraphApiAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        // Scope for Microsoft Graph API
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        // Initialize the confidential client application
        var confidentialClient = ConfidentialClientApplicationBuilder
            .Create(_graphApiOptions.ClientId)
            .WithClientSecret(_graphApiOptions.ClientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_graphApiOptions.TenantId}"))
            .Build();

        // Acquire an access token
        var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken);

        using var httpClient = new HttpClient();

        // Add the access token to the Authorization header
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        // Microsoft Graph API endpoint for sending email
        var endpoint = "https://graph.microsoft.com/v1.0/users/" + _emailOptions.FromEmail + "/sendMail";

        // Create the email message
        var emailMessage = new
        {
            Message = new
            {
                Subject = subject,
                Body = new
                {
                    ContentType = "HTML",
                    Content = body
                },
                ToRecipients = new[]
                {
                    new { EmailAddress = new { Address = toEmail } }
                },
                From = new
                {
                    EmailAddress = new { Address = _emailOptions.FromEmail }
                }
            }
        };

        // Convert email message to JSON
        var jsonContent = JsonConvert.SerializeObject(emailMessage);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Send the POST request
        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {Recipient}. Subject: {Subject}, Provider: Microsoft Graph API", toEmail, subject);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send email using Microsoft Graph API to {Recipient}. Subject: {Subject}, Status Code: {StatusCode}, Error: {Error}",
                             toEmail, subject, response.StatusCode, error);
        }
    }
}
