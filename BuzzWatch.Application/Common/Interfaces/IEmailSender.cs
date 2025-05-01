namespace BuzzWatch.Application.Common.Interfaces;

/// <summary>
/// Interface for sending emails
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken);
} 