using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using SMSRateLimiter.Core.Interfaces;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

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
    
    // Thread-safe collection to store metrics
    private static readonly ConcurrentBag<MessageMetric> _messageMetrics = new();
    
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
        
        // Add to our metrics collection
        _messageMetrics.Add(metric);
        
        _logger.LogInformation($"Received metric for {metric.PhoneNumber}: {metric.RequestCount} requests, {metric.AcceptedCount} accepted, {metric.RejectedCount} rejected");
        
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
        var filteredMetrics = _messageMetrics.AsEnumerable();
        
        // Apply filters
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
        
        // Limit the result to the last 1000 entries if no date range is specified
        // to prevent too much data being sent
        if (!startDate.HasValue && !endDate.HasValue)
        {
            filteredMetrics = filteredMetrics.OrderByDescending(m => m.Timestamp).Take(1000);
        }
        
        return Ok(filteredMetrics.ToList());
    }

    /// <summary>
    /// Gets a list of all phone numbers that have metrics
    /// </summary>
    /// <returns>List of phone numbers</returns>
    [HttpGet("phones")]
    public IActionResult GetPhoneNumbers()
    {
        var phoneNumbers = _messageMetrics
            .Select(m => m.PhoneNumber)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
        
        return Ok(phoneNumbers);
    }
    
    /// <summary>
    /// Gets aggregate metrics for monitoring
    /// </summary>
    /// <param name="minutes">Number of minutes to look back (default: 10)</param>
    /// <returns>Aggregate metrics</returns>
    [HttpGet("summary")]
    public IActionResult GetMetricsSummary([FromQuery] int minutes = 10)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        var recentMetrics = _messageMetrics.Where(m => m.Timestamp >= cutoffTime);
        
        // Group by phone number
        var phoneGroups = recentMetrics
            .GroupBy(m => m.PhoneNumber)
            .Select(g => new
            {
                PhoneNumber = g.Key,
                TotalRequests = g.Sum(m => m.RequestCount),
                AcceptedRequests = g.Sum(m => m.AcceptedCount),
                RejectedRequests = g.Sum(m => m.RejectedCount)
            })
            .ToList();
        
        // Calculate global totals
        var globalTotals = new
        {
            TotalRequests = phoneGroups.Sum(g => g.TotalRequests),
            AcceptedRequests = phoneGroups.Sum(g => g.AcceptedRequests),
            RejectedRequests = phoneGroups.Sum(g => g.RejectedRequests),
            PhoneNumberCount = phoneGroups.Count,
            TimeRangeMinutes = minutes
        };
        
        return Ok(new
        {
            Global = globalTotals,
            PhoneNumbers = phoneGroups
        });
    }
    
    /// <summary>
    /// Clears all stored metrics (for testing/debug purposes)
    /// </summary>
    /// <returns>Success response</returns>
    [HttpDelete("clear")]
    public IActionResult ClearMetrics()
    {
        var oldCount = _messageMetrics.Count;
        
        // Clear all metrics - create a new empty collection
        while (_messageMetrics.TryTake(out _)) { }
        
        return Ok(new { message = $"Cleared {oldCount} metrics" });
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