using Microsoft.AspNetCore.Mvc;
using SMSRateLimiter.API.Models;
using SMSRateLimiter.Core.Interfaces;

namespace SMSRateLimiter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RateLimiterController : ControllerBase
{
    private readonly IRateLimiterService _rateLimiterService;

    public RateLimiterController(IRateLimiterService rateLimiterService)
    {
        _rateLimiterService = rateLimiterService;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckRateLimit([FromBody] RateLimitCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ErrorResponse{Message = "Phone number is required"});
        }

        var canSendMessage = await _rateLimiterService.AllowRequest(request.PhoneNumber);

        return Ok(new RateLimitCheckResponse {
            PhoneNumber = request.PhoneNumber,
            Allowed = canSendMessage
        });
    }

}