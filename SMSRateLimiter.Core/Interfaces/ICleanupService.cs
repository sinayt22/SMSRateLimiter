using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.Interfaces;

public interface ICleanupService
{
    void RegisterForCleanup(string phoneNumber, TokenBucket tokenBucket);
    void StartCleanupTask();
}