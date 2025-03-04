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
}