using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using SMSRateLimiter.Core.RateLimiting;

namespace SMSRateLimiter.Core.RateLimiting.Tests;
public class TokenBucketTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithFullBucket()
    {
        var bucket = new TokenBucket(100, 10);

        // Assert - indirect testing via AllowRequest
        Assert.True(bucket.AllowRequest(100));
        Assert.False(bucket.AllowRequest(1)); // Bucket should be empty now
    }

    [Fact]
    public void AllowRequest_WithAvailableTokens_ShouldReturnTrue()
    {
        // Arrange
        var bucket = new TokenBucket(10, 1);

        // Act
        var result = bucket.AllowRequest(5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AllowRequest_WithInsufficientTokens_ShouldReturnFalse()
    {
        // Arrange
        var bucket = new TokenBucket(5, 1);

        // Act
        var result = bucket.AllowRequest(10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AllowRequest_WithZeroOrNegativeTokens_ShouldThrowException()
    {
        // Arrange
        var bucket = new TokenBucket(10, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => bucket.AllowRequest(0));
        Assert.Throws<ArgumentException>(() => bucket.AllowRequest(-1));
    }

    [Fact]
    public void Refill_ShouldReplenishTokensOverTime()
    {
        // Arrange
        var bucket = new TokenBucket(10, 2); // 2 tokens per second

        // Act - Consume all tokens
        for (int i = 0; i < 10; i++)
        {
            Assert.True(bucket.AllowRequest(1));
        }

        // Bucket should be empty now
        Assert.False(bucket.AllowRequest(1));

        // Wait for refill - should get about 2 tokens in 1 second
        Thread.Sleep(1000);

        // Assert - should have at least 1 token now (allowing for timing variations)
        Assert.True(bucket.AllowRequest(1));
    }

    [Fact]
    public void Refill_ShouldNotExceedMaxCapacity()
    {
        // Arrange
        var bucket = new TokenBucket(5, 10); // 10 tokens per second, max 5

        // Act - Consume all tokens
        Assert.True(bucket.AllowRequest(5));
        Assert.False(bucket.AllowRequest(1)); // Bucket should be empty

        // Wait for refill - at 10 tokens/second, we'd get 10 tokens in 1 second
        // but max capacity is 5
        Thread.Sleep(1000);

        // Assert - should have 5 tokens (max capacity)
        Assert.True(bucket.AllowRequest(5));
        Assert.False(bucket.AllowRequest(1)); // Bucket should be empty again
    }

    [Fact]
    public void AllowRequest_UnderHighConcurrency_ShouldNotOverconsume()
    {
        // Arrange
        var bucket = new TokenBucket(100, 10);
        var successCount = 0;

        // Act - Simulate multiple concurrent requests
        var threads = new Thread[10];
        for (int t = 0; t < threads.Length; t++)
        {
            threads[t] = new Thread(() =>
            {
                for (int i = 0; i < 15; i++)
                {
                    if (bucket.AllowRequest(1))
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
            });
        }

        // Start all threads simultaneously
        foreach (var thread in threads)
        {
            thread.Start();
        }

        // Wait for all threads to complete
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert - should not allow more than the max bucket size
        Assert.Equal(100, successCount);
    }

    [Fact]
    public async Task AllowRequestAsync_WithAvailableTokens_ShouldReturnTrue()
    {
        // Arrange
        var bucket = new TokenBucket(10, 1);

        // Act
        var result = await bucket.AllowRequestAsync(5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AllowRequestAsync_WithInsufficientTokens_ShouldReturnFalse()
    {
        // Arrange
        var bucket = new TokenBucket(5, 1);

        // Act
        var result = await bucket.AllowRequestAsync(10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AllowRequestAsync_WithZeroOrNegativeTokens_ShouldThrowException()
    {
        // Arrange
        var bucket = new TokenBucket(10, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await bucket.AllowRequestAsync(0));
        await Assert.ThrowsAsync<ArgumentException>(async () => await bucket.AllowRequestAsync(-1));
    }

    [Fact]
    public async Task GetCurrentTokensAsync_ShouldReturnCurrentTokenCount()
    {
        // Arrange
        var bucket = new TokenBucket(10, 2); // 2 tokens per second

        // Act & Assert
        var initialTokens = await bucket.GetCurrentTokensAsync();
        Assert.Equal(10, initialTokens);

        // Consume some tokens
        await bucket.AllowRequestAsync(3);

        var remainingTokens = await bucket.GetCurrentTokensAsync();
        Assert.Equal(7, remainingTokens);
    }

    [Fact]
    public async Task GetCurrentTokensAsync_ShouldShowRefill()
    {
        // Arrange
        var bucket = new TokenBucket(10, 2); // 2 tokens per second

        // Consume all tokens
        for (int i = 0; i < 10; i++)
        {
            Assert.True(await bucket.AllowRequestAsync(1));
        }

        var emptyTokens = await bucket.GetCurrentTokensAsync();
        Assert.Equal(0, emptyTokens);

        // Wait for refill
        await Task.Delay(1000);

        // Should have around 2 tokens now (allowing for small timing variations)
        var refillTokens = await bucket.GetCurrentTokensAsync();
        Assert.True(refillTokens >= 1.9 && refillTokens <= 2.1);
    }

    [Fact]
    public async Task AllowRequestAsync_UnderHighConcurrency_ShouldNotOverconsume()
    {
        // Arrange
        var bucket = new TokenBucket(100, 10);
        var successCount = 0;
        var tasks = new List<Task>();

        // Act - Simulate multiple concurrent requests
        for (int t = 0; t < 10; t++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < 15; i++)
                {
                    if (await bucket.AllowRequestAsync(1))
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - should not allow more than the max bucket size
        Assert.Equal(100, successCount);
    }
}