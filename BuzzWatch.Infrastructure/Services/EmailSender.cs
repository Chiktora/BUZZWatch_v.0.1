using BuzzWatch.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BuzzWatch.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;
    
    public EmailSender(
        ILogger<EmailSender> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var host = smtpSettings["Host"] ?? "localhost";
            var port = int.Parse(smtpSettings["Port"] ?? "25");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var from = smtpSettings["From"] ?? "notifications@buzzwatch.com";
            var useSSL = bool.Parse(smtpSettings["UseSsl"] ?? "false");
            
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSSL,
                Credentials = new NetworkCredential(username, password)
            };
            
            var message = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = true
            };
            
            _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
            
            // In development, we might want to just log the email instead of sending it
            if (_configuration["Environment"] == "Development")
            {
                _logger.LogInformation("Email body: {Body}", body);
                return;
            }
            
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }
} 