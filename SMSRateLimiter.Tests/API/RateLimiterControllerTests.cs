using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SMSRateLimiter.API.Controllers;
using SMSRateLimiter.API.Models;
using SMSRateLimiter.Core.Interfaces;
using Xunit;

namespace SMSRateLimiter.API.Tests.Controllers;

public class RateLimiterControllerTests
{
    private readonly Mock<IRateLimiterService> _rateLimiterServiceMock;
    private readonly RateLimiterController _controller;

    public RateLimiterControllerTests()
    {
        _rateLimiterServiceMock = new Mock<IRateLimiterService>();
        _controller = new RateLimiterController(_rateLimiterServiceMock.Object);
    }

    [Fact]
    public async Task CheckRateLimit_WithEmptyPhoneNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = new RateLimitCheckRequest { PhoneNumber = string.Empty };

        // Act
        var result = await _controller.CheckRateLimit(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("Phone number is required", errorResponse.Message);
    }

    [Fact]
    public async Task CheckRateLimit_WithNullPhoneNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = new RateLimitCheckRequest { PhoneNumber = null! };

        // Act
        var result = await _controller.CheckRateLimit(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("Phone number is required", errorResponse.Message);
    }

    [Fact]
    public async Task CheckRateLimit_WhenRateLimiterAllows_ReturnsOkWithAllowedTrue()
    {
        // Arrange
        var phoneNumber = "+1234567890";
        var request = new RateLimitCheckRequest { PhoneNumber = phoneNumber };
        
        _rateLimiterServiceMock.Setup(x => x.AllowRequest(phoneNumber))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckRateLimit(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RateLimitCheckResponse>(okResult.Value);
        
        Assert.Equal(phoneNumber, response.PhoneNumber);
        Assert.True(response.Allowed);
        
        _rateLimiterServiceMock.Verify(x => x.AllowRequest(phoneNumber), Times.Once);
    }

    [Fact]
    public async Task CheckRateLimit_WhenRateLimiterDenies_ReturnsOkWithAllowedFalse()
    {
        // Arrange
        var phoneNumber = "+1234567890";
        var request = new RateLimitCheckRequest { PhoneNumber = phoneNumber };
        
        _rateLimiterServiceMock.Setup(x => x.AllowRequest(phoneNumber))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckRateLimit(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RateLimitCheckResponse>(okResult.Value);
        
        Assert.Equal(phoneNumber, response.PhoneNumber);
        Assert.False(response.Allowed);
        
        _rateLimiterServiceMock.Verify(x => x.AllowRequest(phoneNumber), Times.Once);
    }

    [Fact]
    public async Task CheckRateLimit_WithWhitespacePhoneNumber_ReturnsBadRequest()
    {
        // Arrange
        var request = new RateLimitCheckRequest { PhoneNumber = "   " };

        // Act
        var result = await _controller.CheckRateLimit(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal("Phone number is required", errorResponse.Message);
    }

    [Fact]
public async Task GetBucketStatus_WithValidPhoneNumber_ReturnsOkWithBucketStatus()
{
    // Arrange
    var phoneNumber = "+1234567890";
    var expectedTokens = 15.5;
    var expectedMaxSize = 20L;
    var expectedRefillRate = 2.0;
    var expectedLastUsed = DateTimeOffset.UtcNow;
    
    var mockTokenBucketProvider = new Mock<ITokenBucketProvider>();
    var mockPhoneBucket = new Mock<ITokenBucket>();
    var mockGlobalBucket = new Mock<ITokenBucket>();
    
    mockTokenBucketProvider.Setup(p => p.GetPhoneNumberBucket(phoneNumber))
        .Returns(mockPhoneBucket.Object);
    mockTokenBucketProvider.Setup(p => p.GetGlobalBucket())
        .Returns(mockGlobalBucket.Object);
    
    mockPhoneBucket.Setup(b => b.GetCurrentTokensAsync())
        .ReturnsAsync(expectedTokens);
    mockPhoneBucket.Setup(b => b.MaxBucketSize)
        .Returns(expectedMaxSize);
    mockPhoneBucket.Setup(b => b.RefillRate)
        .Returns(expectedRefillRate);
    mockPhoneBucket.Setup(b => b.LastUsed)
        .Returns(expectedLastUsed);
    
    mockGlobalBucket.Setup(b => b.GetCurrentTokensAsync())
        .ReturnsAsync(90.0);
    mockGlobalBucket.Setup(b => b.MaxBucketSize)
        .Returns(100L);
    mockGlobalBucket.Setup(b => b.RefillRate)
        .Returns(10.0);
    mockGlobalBucket.Setup(b => b.LastUsed)
        .Returns(expectedLastUsed);
    
    // Create a new controller with our mocked services
    var controller = new RateLimiterController(_rateLimiterServiceMock.Object);
    
    // Setup HttpContext and RequestServices to return our mocked provider
    var httpContext = new DefaultHttpContext();
    var serviceProvider = new Mock<IServiceProvider>();
    serviceProvider.Setup(x => x.GetService(typeof(ITokenBucketProvider)))
        .Returns(mockTokenBucketProvider.Object);
    httpContext.RequestServices = serviceProvider.Object;
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = httpContext
    };
    
    // Act
    var result = await controller.GetBucketStatus(phoneNumber);
    
    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<BucketStatusResponse>(okResult.Value);
    
    Assert.Equal(phoneNumber, response.PhoneNumber);
    Assert.Equal(expectedTokens, response.PhoneNumberCurrentTokens);
    Assert.Equal(expectedMaxSize, response.PhoneNumberMaxTokens);
    Assert.Equal(expectedRefillRate, response.PhoneNumberRefillRate);
    Assert.Equal(expectedLastUsed, response.PhoneNumberLastUsed);
    
    Assert.Equal(90.0, response.GlobalCurrentTokens);
    Assert.Equal(100L, response.GlobalMaxTokens);
    Assert.Equal(10.0, response.GlobalRefillRate);
    Assert.Equal(expectedLastUsed, response.GlobalLastUsed);
}

[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public async Task GetBucketStatus_WithInvalidPhoneNumber_ReturnsBadRequest(string phoneNumber)
{
    // Arrange
    var controller = new RateLimiterController(_rateLimiterServiceMock.Object);
    
    // Act
    var result = await controller.GetBucketStatus(phoneNumber);
    
    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
    Assert.Equal("Phone number is required", errorResponse.Message);
}

[Fact]
public async Task GetBucketStatus_WithInvalidPhoneNumberFormat_ReturnsBadRequest()
{
    // Arrange
    var invalidPhoneNumber = "invalid-phone";
    var controller = new RateLimiterController(_rateLimiterServiceMock.Object);
    
    // Act
    var result = await controller.GetBucketStatus(invalidPhoneNumber);
    
    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
    Assert.Equal("Invalid phone number format", errorResponse.Message);
}
}