using System.Collections.Concurrent;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace SMSRateLimiter.Core.Services;

public class LocalTokenBucketProvider : ITokenBucketProvider, IHostedService, IDisposable
{
    // we can convert to use the interface and use the factory pattern to generate them
    // but keeping it simple for now and using the concrete implementation
    private readonly ConcurrentDictionary<string, TokenBucket> _phoneNumberBuckets;
    private readonly TokenBucket _globalBucket;
    private readonly RateLimiterConfig _config;
    private Timer? _cleanupTimer;

    public LocalTokenBucketProvider(RateLimiterConfig config)
    {
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

        return bucket;

    }

    public TokenBucket CreatePhoneNumberBucket(string phoneNumber)
    {
        var bucket = new TokenBucket(refillRate: _config.PhoneNumberRefillRatePerSecond,
                                    maxBucketSize: _config.PhoneNumberMaxRateLimit);

        return bucket;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer = new Timer(
            DoCleanup, null, 
            _config.BucketCleanupIntervalMilliSec, 
            _config.BucketCleanupIntervalMilliSec
            );
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void DoCleanup(object? state)
    {
        var timeoutThreshold = DateTime.UtcNow.AddMilliseconds(-_config.InactiveBucketTimeoutMilliSec);
        
        foreach (var phoneNumber in _phoneNumberBuckets.Keys)
        {
            if (_phoneNumberBuckets.TryGetValue(phoneNumber, out var bucket))
            {
                if (bucket.LastUsed < timeoutThreshold)
                {
                    _phoneNumberBuckets.TryRemove(phoneNumber, out _);
                }
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}


