using SMSRateLimiter.Core.Interfaces;

namespace SMSRateLimiter.Core.Services;

public class RateLimiterService : IRateLimiterService
{
    private readonly ITokenBucketProvider _tokenBucketProvider;
    private readonly SemaphoreSlim _serviceLock = new SemaphoreSlim(1, 1);    
    public RateLimiterService(ITokenBucketProvider tokenBucketProvider)
    {
        _tokenBucketProvider = tokenBucketProvider;
    }
    public async Task<bool> AllowRequest(string phoneNumber)
    {
        var phoneBucket = _tokenBucketProvider.GetPhoneNumberBucket(phoneNumber);
        var globalBucket = _tokenBucketProvider.GetGlobalBucket();
        
        await _serviceLock.WaitAsync();
        
        try
        {
            var phoneTokens = await phoneBucket.GetCurrentTokensAsync();
            var globalTokens = await globalBucket.GetCurrentTokensAsync();
            if (phoneTokens >= 1 && globalTokens >= 1)
            {
                await phoneBucket.AllowRequestAsync(1);
                await globalBucket.AllowRequestAsync(1);
                return true;
            }

            return false;
        }
        finally
        {
            _serviceLock.Release();
        }
    }
}