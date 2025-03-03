using System.Security.Cryptography.X509Certificates;
using SMSRateLimiter.Core.Interfaces;

namespace SMSRateLimiter.Core.RateLimiting;



public class TokenBucket : ITokenBucket
{
    private readonly long _maxBucketSize;
    private readonly double _refillRate;
    private double _currentBucketSize;
    private long _lastRefillTimestamp;
    private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);
    private DateTimeOffset LastUsed { get; set; }
    public TokenBucket(long maxBucketSize, long refillRate)
    {
        _maxBucketSize = maxBucketSize;
        _refillRate = refillRate;
        _currentBucketSize = maxBucketSize;
        LastUsed = DateTimeOffset.UtcNow;
        _lastRefillTimestamp = LastUsed.ToUnixTimeMilliseconds();
    }
    public bool AllowRequest(int tokens = 1)
    {
        if (tokens <= 0)
        {
            throw new ArgumentException("Token count must be positive", nameof(tokens));
        }

        _asyncLock.Wait();
        try
        {
            Refill();
            if (_currentBucketSize >= tokens)
            {
                _currentBucketSize -= tokens;
                LastUsed = DateTimeOffset.UtcNow;
                return true;
            }

            return false;
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    public async Task<bool> AllowRequestAsync(int tokens = 1)
    {
        if (tokens <= 0)
        {
            throw new ArgumentException("Token count must be positive", nameof(tokens));
        }

        await _asyncLock.WaitAsync();
        try
        {
            Refill();
            if (_currentBucketSize >= tokens)
            {
                _currentBucketSize -= tokens;
                LastUsed = DateTimeOffset.UtcNow;
                return true;
            }

            return false;
        }
        finally
        {
            _asyncLock.Release();
        }

    }
    private void Refill()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        double elapsedSeconds = (now - _lastRefillTimestamp) / 1000.0;

        if (elapsedSeconds > 0)
        {
            double tokensToAdd = elapsedSeconds * _refillRate;
            _currentBucketSize = Math.Min(_maxBucketSize, _currentBucketSize + tokensToAdd);
            _lastRefillTimestamp = now;
        }
    }

    public async Task<double> GetCurrentTokensAsync()
    {
        await _asyncLock.WaitAsync();
        try
        {
            Refill();
            return _currentBucketSize;
        }
        finally
        {
            _asyncLock.Release();
        }
    }
}