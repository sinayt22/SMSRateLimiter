using Microsoft.AspNetCore.Mvc;
using SMSRateLimiter.API.Models;
using SMSRateLimiter.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SMSRateLimiter.API.Controllers;

/// <summary>
/// Controller for SMS rate limiting operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RateLimiterController : ControllerBase
{
    private readonly IRateLimiterService _rateLimiterService;
    private readonly ILogger<RateLimiterController> _logger;
    private static readonly Regex PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);
    private readonly IHttpClientFactory _clientFactory;

    /// <summary>
    /// Creates a new instance of the RateLimiterController
    /// </summary>
    /// <param name="rateLimiterService">The rate limiter service</param>
    /// <param name="logger">The logger</param>
    public RateLimiterController(
        IRateLimiterService rateLimiterService,
        ILogger<RateLimiterController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _rateLimiterService = rateLimiterService;
        _logger = logger;
        _clientFactory = httpClientFactory;
    }

    /// <summary>
    /// Checks if a message can be sent to the specified phone number based on rate limits
    /// </summary>
    /// <param name="request">The rate limit check request containing the phone number</param>
    /// <returns>A response indicating whether the message is allowed</returns>
    /// <response code="200">Returns the rate limit check result</response>
    /// <response code="400">If the phone number is invalid or empty</response>
    [HttpPost("check")]
    [ProducesResponseType(typeof(RateLimitCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckRateLimit([FromBody] RateLimitCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ErrorResponse { Message = "Phone number is required" });
        }

        if (!PhoneRegex.IsMatch(request.PhoneNumber))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid phone number format" });
        }

        var canSendMessage = await _rateLimiterService.AllowRequest(request.PhoneNumber);
        
        // Record the check result for metrics
        try
        {
            await RecordMetricAsync(request.PhoneNumber, canSendMessage);
        }
        catch (Exception ex)
        {
            // Log but don't fail the request if metrics recording fails
            _logger.LogError(ex, "Failed to record metric for {PhoneNumber}", request.PhoneNumber);
        }

        return Ok(new RateLimitCheckResponse
        {
            PhoneNumber = request.PhoneNumber,
            Allowed = canSendMessage
        });
    }

    /// <summary>
    /// Gets the current status of rate limiting buckets for a phone number
    /// </summary>
    /// <param name="phoneNumber">The phone number to check</param>
    /// <returns>The current status of phone number and global buckets</returns>
    /// <response code="200">Returns the bucket status information</response>
    /// <response code="400">If the phone number is invalid or empty</response>
    [HttpGet("status/{phoneNumber}")]
    [ProducesResponseType(typeof(BucketStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBucketStatus(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new ErrorResponse { Message = "Phone number is required" });
        }

        if (!PhoneRegex.IsMatch(phoneNumber))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid phone number format" });
        }

        // Get the buckets
        var tokenBucketProvider = HttpContext.RequestServices.GetRequiredService<ITokenBucketProvider>();
        var phoneBucket = tokenBucketProvider.GetPhoneNumberBucket(phoneNumber);
        var globalBucket = tokenBucketProvider.GetGlobalBucket();

        // Get current tokens
        var phoneTokens = await phoneBucket.GetCurrentTokensAsync();
        var globalTokens = await globalBucket.GetCurrentTokensAsync();

        return Ok(new BucketStatusResponse
        {
            PhoneNumber = phoneNumber,
            PhoneNumberCurrentTokens = phoneTokens,
            PhoneNumberMaxTokens = phoneBucket.MaxBucketSize,
            PhoneNumberRefillRate = phoneBucket.RefillRate,
            GlobalCurrentTokens = globalTokens,
            GlobalMaxTokens = globalBucket.MaxBucketSize,
            GlobalRefillRate = globalBucket.RefillRate,
            PhoneNumberLastUsed = phoneBucket.LastUsed,
            GlobalLastUsed = globalBucket.LastUsed
        });
    }
    
    /// <summary>
    /// Records a metric for a rate limit check
    /// </summary>
    private async Task RecordMetricAsync(string phoneNumber, bool allowed)
    {
        // Create the metric object
        var metric = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            PhoneNumber = phoneNumber,
            RequestCount = 1,
            AcceptedCount = allowed ? 1 : 0,
            RejectedCount = allowed ? 0 : 1
        };
        
        // Get the base address from current request
        var baseAddress = $"{Request.Scheme}://{Request.Host}";
        
        // Send to metrics endpoint
        var client = _clientFactory.CreateClient("MetricsClient");
        var response = await client.PostAsJsonAsync(
            $"{baseAddress}/api/Metrics/messages", 
            metric);
            
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to record metric: {StatusCode} - {Error}", 
                response.StatusCode, error);
        }
    }
}