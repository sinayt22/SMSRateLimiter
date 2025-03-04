using System.Threading.Tasks;

namespace SMSRateLimiter.Core.Interfaces;

public interface IRateLimiterService
{
    Task<bool> AllowRequest(string phoneNumber);
}
