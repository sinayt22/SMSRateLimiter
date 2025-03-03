using Moq;
using Xunit;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.Services.Tests;

public class InMemoryTokenBucketProviderTests
{
    private readonly Mock<ICleanupService> _mockCleanupService;
    private readonly RateLimiterConfig _config;
    private readonly LocalTokenBucketProvider _provider;

    public InMemoryTokenBucketProviderTests()
    {
        _mockCleanupService = new Mock<ICleanupService>();
        _config = new RateLimiterConfig
        {
            GlobalRateMaxRateLimit = 100,
            GlobalRefillRatePerSecond = 10,
            PhoneNumberMaxRateLimit = 20,
            PhoneNumberRefillRatePerSecond = 2
        };
        _provider = new LocalTokenBucketProvider(_mockCleanupService.Object, _config);
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
        _mockCleanupService.Verify(s => s.RegisterForCleanup(phoneNumber, It.IsAny<TokenBucket>()), Times.Once);
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
        _mockCleanupService.Verify(s => s.RegisterForCleanup(phoneNumber, It.IsAny<TokenBucket>()), Times.Exactly(2));
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
}