namespace SMSRateLimiter.Tests.LoadTest;

/// <summary>
/// Strategy for distributing requests across phone numbers
/// </summary>
public enum PhoneNumberDistribution
{
    /// <summary>
    /// Cycle through phone numbers in sequence
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Choose phone numbers randomly with equal probability
    /// </summary>
    Random,
    
    /// <summary>
    /// Choose phone numbers with a weighted distribution (80/20 rule)
    /// </summary>
    WeightedRandom
}

