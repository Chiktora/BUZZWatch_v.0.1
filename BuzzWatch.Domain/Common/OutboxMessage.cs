namespace BuzzWatch.Domain.Common;

/// <summary>
/// Represents a message in the outbox for reliable messaging
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier of the message
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Message type (often the event or command name)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// The content of the message (typically JSON)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When the message was processed (null if not yet processed)
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }
    
    /// <summary>
    /// Status of the message (Pending, Processed, Failed)
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }
} 