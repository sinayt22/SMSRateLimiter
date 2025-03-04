namespace SMSRateLimiter.Core.Configuration;

public class RateLimiterConfig
{
    public int PhoneNumberMaxRateLimit { get; set; }
    public int GlobalRateMaxRateLimit { get; set; }
    public int PhoneNumberRefillRatePerSecond {get; set;}
    public int GlobalRefillRatePerSecond {get; set;}

    public int BucketCleanupIntervalMilliSec { get; set; } = 60 * 1000; // 1 minute in milliseconds
    public int InactiveBucketTimeoutMilliSec { get; set; } = 120 * 1000; // 2 minutes in milliseconds
}