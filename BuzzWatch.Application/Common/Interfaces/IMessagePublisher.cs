namespace BuzzWatch.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing messages to external services
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message of the specified type
    /// </summary>
    /// <param name="type">Message type</param>
    /// <param name="content">Message content (serialized JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync(string type, string content, CancellationToken cancellationToken);
} 