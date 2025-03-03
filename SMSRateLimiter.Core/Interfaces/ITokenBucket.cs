namespace SMSRateLimiter.Core.Interfaces;

public interface ITokenBucket
{
    bool AllowRequest(int tokens);
    Task<bool> AllowRequestAsync(int tokens);
    Task<double> GetCurrentTokensAsync();
}