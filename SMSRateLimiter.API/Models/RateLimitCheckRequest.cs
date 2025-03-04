namespace SMSRateLimiter.API.Models;

public class RateLimitCheckRequest
{
    public required string PhoneNumber {get; set;}
}