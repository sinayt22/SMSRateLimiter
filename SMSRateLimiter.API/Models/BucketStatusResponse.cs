namespace SMSRateLimiter.API.Models;

public class BucketStatusResponse
{
    public required string PhoneNumber { get; set; }
    public double PhoneNumberCurrentTokens { get; set; }
    public long PhoneNumberMaxTokens { get; set; }
    public double PhoneNumberRefillRate { get; set; }
    public double GlobalCurrentTokens { get; set; }
    public long GlobalMaxTokens { get; set; }
    public double GlobalRefillRate { get; set; }
    public DateTimeOffset PhoneNumberLastUsed { get; set; }
    public DateTimeOffset GlobalLastUsed { get; set; }
}