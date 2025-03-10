namespace SMSRateLimiter.API.Models;

/// <summary>
/// Represents a message metric record
/// </summary>
public class MessageMetric
{
    /// <summary>
    /// Gets or sets the timestamp when the metric was recorded
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the phone number
    /// </summary>
    public required string PhoneNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of requests
    /// </summary>
    public int RequestCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of accepted requests
    /// </summary>
    public int AcceptedCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of rejected requests
    /// </summary>
    public int RejectedCount { get; set; }
}