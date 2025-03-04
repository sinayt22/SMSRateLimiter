namespace SMSRateLimiter.API.Models;

public class RateLimitCheckResponse
{
    public required string PhoneNumber {get; set;}
    public bool Allowed {get; set;}
}