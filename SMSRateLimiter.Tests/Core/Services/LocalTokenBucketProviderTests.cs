using Moq;
using Xunit;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.Services.Tests;

public class LocalTokenBucketProviderTests
{
    private readonly RateLimiterConfig _config;
    private readonly LocalTokenBucketProvider _provider;

    public LocalTokenBucketProviderTests()
    {
        _config = new RateLimiterConfig
        {
            GlobalRateMaxRateLimit = 100,
            GlobalRefillRatePerSecond = 10,
            PhoneNumberMaxRateLimit = 20,
            PhoneNumberRefillRatePerSecond = 2,
            BucketCleanupIntervalMilliSec = 1000,
            InactiveBucketTimeoutMilliSec = 5000
        };
        _provider = new LocalTokenBucketProvider(_config);
    }

    [Fact]
    public void GetGlobalBucket_ReturnsConfiguredBucket()
    {
        // Act
        var bucket = _provider.GetGlobalBucket();

        // Assert
        Assert.NotNull(bucket);
        Assert.Equal(_config.GlobalRateMaxRateLimit, bucket.MaxBucketSize);
        Assert.Equal(_config.GlobalRefillRatePerSecond, bucket.RefillRate);
    }

    [Fact]
    public void GetPhoneNumberBucket_WithValidPhoneNumber_ReturnsBucket()
    {
        // Arrange
        var phoneNumber = "+1234567890";

        // Act
        var bucket = _provider.GetPhoneNumberBucket(phoneNumber);

        // Assert
        Assert.NotNull(bucket);
        Assert.Equal(_config.PhoneNumberMaxRateLimit, bucket.MaxBucketSize);
        Assert.Equal(_config.PhoneNumberRefillRatePerSecond, bucket.RefillRate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetPhoneNumberBucket_WithInvalidPhoneNumber_ThrowsArgumentException(string phoneNumber)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _provider.GetPhoneNumberBucket(phoneNumber));
        Assert.Equal("Phone number cannot be empty (Parameter 'phoneNumber')", exception.Message);
    }

    [Fact]
    public void GetPhoneNumberBucket_CalledMultipleTimes_ReturnsSameBucketInstance()
    {
        // Arrange
        var phoneNumber = "+1234567890";

        // Act
        var bucket1 = _provider.GetPhoneNumberBucket(phoneNumber);
        var bucket2 = _provider.GetPhoneNumberBucket(phoneNumber);

        // Assert
        Assert.Same(bucket1, bucket2);
    }

    [Fact]
    public void CreatePhoneNumberBucket_ReturnsNewBucketWithCorrectConfiguration()
    {
        // Arrange
        var phoneNumber = "+1234567890";

        // Act
        var bucket = _provider.CreatePhoneNumberBucket(phoneNumber);

        // Assert
        Assert.NotNull(bucket);
        Assert.Equal(_config.PhoneNumberMaxRateLimit, bucket.MaxBucketSize);
        Assert.Equal(_config.PhoneNumberRefillRatePerSecond, bucket.RefillRate);
    }

    [Fact]
    public async Task StartAsync_InitializesCleanupTimer()
    {
        // Act
        await _provider.StartAsync(CancellationToken.None);

        // Assert
        // Add a bucket and wait for cleanup
        var phoneNumber = "+1234567890";
        var bucket = _provider.GetPhoneNumberBucket(phoneNumber);
        
        // Simulate passage of time by reflecting the LastUsed property
        var lastUsedField = bucket.GetType().GetField("_lastUsed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lastUsedField?.SetValue(bucket, DateTime.UtcNow.AddSeconds(-20));

        // Wait for cleanup cycle
        await Task.Delay(_config.BucketCleanupIntervalMilliSec * 10);

        // Try to get the bucket again - should be a new instance
        var newBucket = _provider.GetPhoneNumberBucket(phoneNumber);
        Assert.NotSame(bucket, newBucket);
    }

    [Fact]
    public async Task StopAsync_StopsCleanupTimer()
    {
        // Arrange
        await _provider.StartAsync(CancellationToken.None);
        var phoneNumber = "+1234567890";
        var bucket = _provider.GetPhoneNumberBucket(phoneNumber);

        // Act
        await _provider.StopAsync(CancellationToken.None);

        // Simulate passage of time
        var lastUsedField = bucket.GetType().GetField("_lastUsed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lastUsedField?.SetValue(bucket, DateTime.UtcNow.AddSeconds(-10));

        // Wait for what would have been a cleanup cycle
        await Task.Delay(_config.BucketCleanupIntervalMilliSec * 2);

        // Assert
        var sameBucket = _provider.GetPhoneNumberBucket(phoneNumber);
        Assert.Same(bucket, sameBucket);
    }

    [Fact]
    public void Dispose_DisposesCleanupTimer()
    {
        // Act & Assert - no exception should be thrown
        _provider.Dispose();
    }
}