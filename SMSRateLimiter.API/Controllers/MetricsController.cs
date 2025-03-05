using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using SMSRateLimiter.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SMSRateLimiter.API.Controllers;

/// <summary>
/// Controller for metrics and monitoring operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ITokenBucketProvider _tokenBucketProvider;
    private readonly ILogger<MetricsController> _logger;
    
    // In a real application, we would use a proper metrics store
    // For demo purposes, we'll use an in-memory collection
    private static readonly List<MessageMetric> _messageMetrics = new();
    private static readonly object _metricsLock = new();
    
    private static readonly List<string> _samplePhoneNumbers = new()
    {
        "+14155551212", "+14155551213", "+14155551214",
        "+16505551212", "+16505551213",
        "+12125551212", "+12125551213", "+12125551214", "+12125551215"
    };

    /// <summary>
    /// Creates a new instance of the MetricsController
    /// </summary>
    /// <param name="tokenBucketProvider">The token bucket provider</param>
    /// <param name="logger">The logger</param>
    public MetricsController(
        ITokenBucketProvider tokenBucketProvider,
        ILogger<MetricsController> logger)
    {
        _tokenBucketProvider = tokenBucketProvider;
        _logger = logger;
        
        // Initialize with some sample data if empty
        if (_messageMetrics.Count == 0)
        {
            InitializeSampleData();
        }
    }

    /// <summary>
    /// Records a new message metric
    /// </summary>
    /// <param name="metric">The message metric to record</param>
    /// <returns>Success response</returns>
    [HttpPost("messages")]
    public IActionResult RecordMessageMetric([FromBody] MessageMetric metric)
    {
        if (string.IsNullOrWhiteSpace(metric.PhoneNumber))
        {
            return BadRequest(new { message = "Phone number is required" });
        }
        
        // Set timestamp if not provided
        if (metric.Timestamp == default)
        {
            metric.Timestamp = DateTimeOffset.UtcNow;
        }
        
        lock (_metricsLock)
        {
            _messageMetrics.Add(metric);
        }
        
        return Ok();
    }

    /// <summary>
    /// Gets message metrics with optional filtering
    /// </summary>
    /// <param name="phoneNumber">Optional phone number filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>Filtered message metrics</returns>
    [HttpGet("messages")]
    public IActionResult GetMessageMetrics(
        [FromQuery] string? phoneNumber = null,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        lock (_metricsLock)
        {
            var filteredMetrics = _messageMetrics.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                filteredMetrics = filteredMetrics.Where(m => m.PhoneNumber == phoneNumber);
            }
            
            if (startDate.HasValue)
            {
                filteredMetrics = filteredMetrics.Where(m => m.Timestamp >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                filteredMetrics = filteredMetrics.Where(m => m.Timestamp <= endDate.Value);
            }
            
            return Ok(filteredMetrics.ToList());
        }
    }

    /// <summary>
    /// Gets a list of all phone numbers
    /// </summary>
    /// <returns>List of phone numbers</returns>
    [HttpGet("phones")]
    public IActionResult GetPhoneNumbers()
    {
        lock (_metricsLock)
        {
            var phoneNumbers = _messageMetrics
                .Select(m => m.PhoneNumber)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
            
            return Ok(phoneNumbers);
        }
    }

    // Generate some sample metrics data for demonstration
    private void InitializeSampleData()
    {
        var random = new Random(42); // Deterministic for demo
        var now = DateTimeOffset.UtcNow;
        
        // Generate metrics for the last 24 hours
        for (int hour = 0; hour < 24; hour++)
        {
            var hourTimestamp = now.AddHours(-24 + hour);
            
            // For each phone number
            foreach (var phoneNumber in _samplePhoneNumbers)
            {
                // Generate metrics for each 5-minute interval
                for (int minute = 0; minute < 60; minute += 5)
                {
                    var timestamp = hourTimestamp.AddMinutes(minute);
                    
                    // Base request count varies by time of day
                    var timeOfDayFactor = 0.5 + Math.Sin((hour / 24.0) * 2 * Math.PI) * 0.5;
                    var baseRequests = (int)(10 * timeOfDayFactor);
                    
                    // Random fluctuation
                    var requests = Math.Max(0, baseRequests + random.Next(-5, 6));
                    
                    // Some rejected requests based on rate limiting
                    var rejected = Math.Min(requests, random.Next(4) == 0 ? random.Next(1, 4) : 0);
                    var accepted = requests - rejected;
                    
                    if (requests > 0)
                    {
                        _messageMetrics.Add(new MessageMetric
                        {
                            Timestamp = timestamp,
                            PhoneNumber = phoneNumber,
                            RequestCount = requests,
                            AcceptedCount = accepted,
                            RejectedCount = rejected
                        });
                    }
                }
            }
        }
    }
}

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