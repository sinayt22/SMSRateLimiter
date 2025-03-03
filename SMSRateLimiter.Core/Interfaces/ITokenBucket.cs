namespace SMSRateLimiter.Core.Interfaces;

public interface ITokenBucket
{
    double RefillRate { get; }
    long MaxBucketSize { get; }
    DateTimeOffset LastUsed {get; set;}

    bool AllowRequest(int tokens = 1);
    Task<bool> AllowRequestAsync(int tokens = 1);
    Task<double> GetCurrentTokensAsync();
}