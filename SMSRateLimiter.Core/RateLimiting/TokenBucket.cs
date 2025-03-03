namespace SMSRateLimiter.Core.RateLimiting;

public class TokenBucket
{
    private readonly long _maxBucketSize;
    private readonly double _refillRate;
    private double _currentBucketSize;
    private long _lastRefillTimestamp;
    private readonly object _lock = new object();
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

        lock (_lock)
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
}