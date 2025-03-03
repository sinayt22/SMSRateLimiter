using Moq;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.Services;
using Xunit;

namespace SMSRateLimiter.Tests;

public class RateLimiterServiceTests
{
    private readonly Mock<ITokenBucketProvider> _tokenBucketProviderMock;
    private readonly Mock<ITokenBucket> _phoneTokenBucketMock;
    private readonly Mock<ITokenBucket> _globalTokenBucketMock;
    private readonly RateLimiterService _rateLimiterService;

    public RateLimiterServiceTests()
    {
        _tokenBucketProviderMock = new Mock<ITokenBucketProvider>();
        _phoneTokenBucketMock = new Mock<ITokenBucket>();
        _globalTokenBucketMock = new Mock<ITokenBucket>();
        
        _tokenBucketProviderMock.Setup(x => x.GetPhoneNumberBucket(It.IsAny<string>()))
            .Returns(_phoneTokenBucketMock.Object);
        _tokenBucketProviderMock.Setup(x => x.GetGlobalBucket())
            .Returns(_globalTokenBucketMock.Object);

        _rateLimiterService = new RateLimiterService(_tokenBucketProviderMock.Object);
    }

    [Fact]
    public async Task AllowRequest_WhenBothBucketsHaveTokens_ShouldReturnTrue()
    {
        // Arrange
        _phoneTokenBucketMock.Setup(x => x.GetCurrentTokensAsync())
            .ReturnsAsync(1);
        _globalTokenBucketMock.Setup(x => x.GetCurrentTokensAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _rateLimiterService.AllowRequest("1234567890");

        // Assert
        Assert.True(result);
        _phoneTokenBucketMock.Verify(x => x.AllowRequestAsync(1), Times.Once);
        _globalTokenBucketMock.Verify(x => x.AllowRequestAsync(1), Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public async Task AllowRequest_WhenEitherBucketHasNoTokens_ShouldReturnFalse(int phoneTokens, int globalTokens)
    {
        // Arrange
        _phoneTokenBucketMock.Setup(x => x.GetCurrentTokensAsync())
            .ReturnsAsync(phoneTokens);
        _globalTokenBucketMock.Setup(x => x.GetCurrentTokensAsync())
            .ReturnsAsync(globalTokens);

        // Act
        var result = await _rateLimiterService.AllowRequest("1234567890");

        // Assert
        Assert.False(result);
        _phoneTokenBucketMock.Verify(x => x.AllowRequestAsync(1), Times.Never);
        _globalTokenBucketMock.Verify(x => x.AllowRequestAsync(1), Times.Never);
    }
}