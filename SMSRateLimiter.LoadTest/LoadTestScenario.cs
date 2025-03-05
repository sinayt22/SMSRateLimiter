namespace SMSRateLimiter.Tests.LoadTest;

/// <summary>
/// Defines a load test scenario
/// </summary>
public class LoadTestScenario
{
    /// <summary>
    /// Scenario name for reporting
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Phone numbers to use in the test
    /// </summary>
    public string[] PhoneNumbers { get; set; }
    
    /// <summary>
    /// Target requests per second
    /// </summary>
    public int RequestsPerSecond { get; set; }
    
    /// <summary>
    /// Distribution strategy for the phone numbers
    /// </summary>
    public PhoneNumberDistribution Distribution { get; set; }
    
    private int _currentIndex = 0;
    private readonly Random _random = new Random();
    
    public string GetNextPhoneNumber()
    {
        return Distribution switch
        {
            PhoneNumberDistribution.Sequential => GetSequentialPhoneNumber(),
            PhoneNumberDistribution.Random => GetRandomPhoneNumber(),
            PhoneNumberDistribution.WeightedRandom => GetWeightedRandomPhoneNumber(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private string GetSequentialPhoneNumber()
    {
        var index = _currentIndex++ % PhoneNumbers.Length;
        return PhoneNumbers[index];
    }
    
    private string GetRandomPhoneNumber()
    {
        var index = _random.Next(PhoneNumbers.Length);
        return PhoneNumbers[index];
    }
    
    private string GetWeightedRandomPhoneNumber()
    {
        // 80% of traffic goes to 20% of numbers
        if (_random.NextDouble() < 0.8)
        {
            // Use the first 20% of numbers
            var hotspotCount = Math.Max(1, PhoneNumbers.Length / 5);
            var index = _random.Next(hotspotCount);
            return PhoneNumbers[index];
        }
        else
        {
            // Use the remaining 80% of numbers
            var hotspotCount = Math.Max(1, PhoneNumbers.Length / 5);
            var index = hotspotCount + _random.Next(PhoneNumbers.Length - hotspotCount);
            return PhoneNumbers[index];
        }
    }
}

