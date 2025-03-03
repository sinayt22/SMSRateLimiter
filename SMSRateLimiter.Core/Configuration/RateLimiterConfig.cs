namespace SMSRateLimiter.Core.Configuration;

public class RateLimiterConfig
{
    public int PhoneNumberMaxRateLimit { get; set; }
    public int GlobalRateMaxRateLimit { get; set; }
    public int PhoneNumberRefillRatePerSecond {get; set;}
    public int GlobalRefillRatePerSecond {get; set;}

    public int BucketCleanupIntervalMinutes { get; set; } = 60;
    public int InactiveBucketTimeoutMinutes { get; set; } = 120;
}