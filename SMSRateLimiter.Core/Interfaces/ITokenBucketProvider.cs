using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.Interfaces;

public interface ITokenBucketProvider
{
   TokenBucket GetPhoneNumberBucket(string phoneNumber);
   TokenBucket GetGlobalBucket();
}