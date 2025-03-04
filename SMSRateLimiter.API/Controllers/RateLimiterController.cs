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
    private static readonly Regex PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);

    /// <summary>
    /// Creates a new instance of the RateLimiterController
    /// </summary>
    /// <param name="rateLimiterService">The rate limiter service</param>
    public RateLimiterController(IRateLimiterService rateLimiterService)
    {
        _rateLimiterService = rateLimiterService;
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

        return Ok(new RateLimitCheckResponse {
            PhoneNumber = request.PhoneNumber,
            Allowed = canSendMessage
        });
    }
}