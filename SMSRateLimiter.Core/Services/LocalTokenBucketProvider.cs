using System.Collections.Concurrent;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.Services;

public class LocalTokenBucketProvider : ITokenBucketProvider
{
    // we can convert to use the interface and use the factory pattern to generate them
    // but keeping it simple for now and using the concrete implementation
    private readonly ConcurrentDictionary<string, TokenBucket> _phoneNumberBuckets;
    private readonly TokenBucket _globalBucket;
    private readonly ICleanupService _cleanupService;
    private readonly RateLimiterConfig _config;

    public LocalTokenBucketProvider(
        ICleanupService cleanupService,
        RateLimiterConfig config)
    {
        _cleanupService = cleanupService;
        _config = config;
        _phoneNumberBuckets = new ConcurrentDictionary<string, TokenBucket>();
        _globalBucket = new TokenBucket(maxBucketSize: _config.GlobalRateMaxRateLimit,
                                        refillRate: _config.GlobalRefillRatePerSecond);

    }

    public ITokenBucket GetGlobalBucket()
    {
        return _globalBucket;
    }
    public ITokenBucket GetPhoneNumberBucket(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
        }

        var bucket = _phoneNumberBuckets.GetOrAdd(phoneNumber, _ => CreatePhoneNumberBucket(phoneNumber));

        _cleanupService.RegisterForCleanup(phoneNumber, bucket);
        return bucket;

    }

    public TokenBucket CreatePhoneNumberBucket(string phoneNumber)
    {
        var bucket = new TokenBucket(refillRate: _config.PhoneNumberRefillRatePerSecond,
                                    maxBucketSize: _config.PhoneNumberMaxRateLimit);

        return bucket;
    }
}


