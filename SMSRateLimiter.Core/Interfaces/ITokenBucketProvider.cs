namespace SMSRateLimiter.Core.Interfaces;

public interface ITokenBucketProvider
{
   ITokenBucket GetPhoneNumberBucket(string phoneNumber);
   ITokenBucket GetGlobalBucket();
}