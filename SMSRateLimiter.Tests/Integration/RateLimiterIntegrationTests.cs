using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SMSRateLimiter.API;
using SMSRateLimiter.API.Models;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.Services;
using Xunit.Abstractions;

namespace SMSRateLimiter.Integration.Tests;

public class RateLimiterIntegrationTests : IClassFixture<WebApplicationFactory<TestProgram>>
{
    private readonly WebApplicationFactory<TestProgram> _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public RateLimiterIntegrationTests(WebApplicationFactory<TestProgram> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override rate limiter config for testing purposes
                services.AddSingleton(new RateLimiterConfig
                {
                    PhoneNumberMaxRateLimit = 5,          // Small bucket size for faster testing
                    GlobalRateMaxRateLimit = 10,          // Small global bucket for testing
                    PhoneNumberRefillRatePerSecond = 1,   // Slow refill rate to test behavior
                    GlobalRefillRatePerSecond = 2,        // Slow global refill rate
                    BucketCleanupIntervalMilliSec = 5000, // 5 seconds
                    InactiveBucketTimeoutMilliSec = 10000 // 10 seconds
                });
            });
        });
        
        _output = output;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SinglePhoneNumber_ExhaustsBucket_ThenRefills()
    {
        // Arrange
        var phoneNumber = "+12345678901";
        var endpoint = "/api/RateLimiter/check";
        var requestContent = JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber });
        
        _output.WriteLine($"Test started at: {DateTime.Now}");
        _output.WriteLine("Testing single phone number bucket exhaustion and refill");
        
        // Act & Assert - Initial status check
        var initialStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"Initial bucket status: {initialStatus.PhoneNumberCurrentTokens} tokens available");
        
        // First, exhaust the bucket (should have 5 tokens)
        var results = new List<bool>();
        for (int i = 0; i < 7; i++) // Try 7 requests (more than our bucket size of 5)
        {
            var response = await _client.PostAsync(endpoint, JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber }));
            var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
            results.Add(result.Allowed);
            _output.WriteLine($"Request {i+1}: Allowed = {result.Allowed}");
        }
        
        // Assert - First 5 should be allowed, next 2 should be denied
        Assert.Equal(5, results.Count(r => r));
        Assert.Equal(2, results.Count(r => !r));
        
        // Check bucket status after exhaustion
        var afterExhaustionStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"After exhaustion: {afterExhaustionStatus.PhoneNumberCurrentTokens} tokens available");
        Assert.True(afterExhaustionStatus.PhoneNumberCurrentTokens < 1.0);
        
        // Wait for refill (1 token per second, so wait 3 seconds for 3 tokens)
        _output.WriteLine("Waiting for bucket to refill...");
        await Task.Delay(3000);
        
        // Check status again
        var afterRefillStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"After refill wait: {afterRefillStatus.PhoneNumberCurrentTokens} tokens available");
        
        // Should have at least 2 tokens now (allowing for slight timing variations)
        Assert.True(afterRefillStatus.PhoneNumberCurrentTokens >= 2.0);
        
        // Try another 3 requests - should be allowed
        var refillResults = new List<bool>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.PostAsync(endpoint, JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber }));
            var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
            refillResults.Add(result.Allowed);
            _output.WriteLine($"Refill request {i+1}: Allowed = {result.Allowed}");
        }
        
        // All 3 should be allowed
        Assert.True(refillResults.All(r => r));
        
        // Check final status
        var finalStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"Final status: {finalStatus.PhoneNumberCurrentTokens} tokens available");
    }

    [Fact]
    public async Task MultiplePhoneNumbers_IndependentBuckets()
    {
        // Arrange
        var phone1 = "+14155551212";
        var phone2 = "+16505551212";
        
        _output.WriteLine("Testing multiple phone numbers with independent buckets");
        
        // Get initial status
        var initialStatus1 = await GetBucketStatus(phone1);
        var initialStatus2 = await GetBucketStatus(phone2);
        
        _output.WriteLine($"Phone 1 initial tokens: {initialStatus1.PhoneNumberCurrentTokens}");
        _output.WriteLine($"Phone 2 initial tokens: {initialStatus2.PhoneNumberCurrentTokens}");
        
        // Act - Exhaust bucket for phone1
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsync("/api/RateLimiter/check", 
                JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phone1 }));
            var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
            _output.WriteLine($"Phone 1 request {i+1}: Allowed = {result.Allowed}");
        }
        
        // Assert - Phone1 should be out of tokens, but Phone2 should still have full tokens
        var afterExhaustionStatus1 = await GetBucketStatus(phone1);
        var afterExhaustionStatus2 = await GetBucketStatus(phone2);
        
        _output.WriteLine($"Phone 1 tokens after exhaustion: {afterExhaustionStatus1.PhoneNumberCurrentTokens}");
        _output.WriteLine($"Phone 2 tokens (untouched): {afterExhaustionStatus2.PhoneNumberCurrentTokens}");
        
        Assert.True(afterExhaustionStatus1.PhoneNumberCurrentTokens < 1.0);
        Assert.Equal(5.0, afterExhaustionStatus2.PhoneNumberCurrentTokens);
        
        // Try one more request with each phone
        var phone1Response = await _client.PostAsync("/api/RateLimiter/check", 
            JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phone1 }));
        var phone1Result = await phone1Response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
        
        var phone2Response = await _client.PostAsync("/api/RateLimiter/check", 
            JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phone2 }));
        var phone2Result = await phone2Response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
        
        _output.WriteLine($"Phone 1 additional request: Allowed = {phone1Result.Allowed}");
        _output.WriteLine($"Phone 2 additional request: Allowed = {phone2Result.Allowed}");
        
        // Phone1 should be denied, Phone2 should be allowed
        Assert.False(phone1Result.Allowed);
        Assert.True(phone2Result.Allowed);
    }

    [Fact]
    public async Task GlobalBucket_SharedAcrossPhoneNumbers()
    {
        // Arrange - We'll use 6 different phone numbers
        var phones = new[]
        {
            "+14155551001",
            "+14155551002",
            "+14155551003",
            "+14155551004",
            "+14155551005",
            "+14155551006",
            "+14155551007",
            "+14155551008",
            "+14155551009",
            "+14155551010",
            "+14155551011",
            "+14155551012",
        };
        
        _output.WriteLine("Testing global bucket shared across phone numbers");
        
        // Check global bucket initial status using first phone
        var initialStatus = await GetBucketStatus(phones[0]);
        _output.WriteLine($"Global bucket initial tokens: {initialStatus.GlobalCurrentTokens}");
        
        // Act - Make 10 requests with different phone numbers (should exhaust global bucket)
        var results = new List<bool>();
        for (int i = 0; i < phones.Length; i++)
        {
            var response = await _client.PostAsync("/api/RateLimiter/check", 
                JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phones[i] }));
            var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
            results.Add(result.Allowed);
            _output.WriteLine($"Phone {i+1}: Allowed = {result.Allowed}");
            
            // Check global bucket status periodically
            if (i % 3 == 0 || i == phones.Length - 1)
            {
                var currentStatus = await GetBucketStatus(phones[0]);
                _output.WriteLine($"Global tokens after {i+1} requests: {currentStatus.GlobalCurrentTokens}");
            }
        }
        
        // Assert - First 10 should succeed, remaining should fail due to global limit
        Assert.Equal(10, results.Count(r => r));
        Assert.Equal(2, results.Count(r => !r));
        
        // Final status check - global bucket should be nearly empty
        var finalStatus = await GetBucketStatus(phones[0]);
        _output.WriteLine($"Global bucket final tokens: {finalStatus.GlobalCurrentTokens}");
        Assert.True(finalStatus.GlobalCurrentTokens < 1.0);
        
        // Now wait for global bucket to refill
        _output.WriteLine("Waiting for global bucket to refill...");
        await Task.Delay(3000); // Should get ~6 tokens back (2/second * 3 seconds)
        
        var afterRefillStatus = await GetBucketStatus(phones[0]);
        _output.WriteLine($"Global bucket tokens after refill: {afterRefillStatus.GlobalCurrentTokens}");
        Assert.True(afterRefillStatus.GlobalCurrentTokens >= 5.0);
    }

    [Fact]
    public async Task HighConcurrency_TokensNotOverconsumed()
    {
        // Arrange
        var phoneNumber = "+15105551212";
        var endpoint = "/api/RateLimiter/check";
        var taskCount = 20; // More tasks than tokens
        
        _output.WriteLine("Testing high concurrency with token bucket");
        
        // Initial status
        var initialStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"Initial tokens: {initialStatus.PhoneNumberCurrentTokens}");
        
        // Reset bucket by waiting if needed
        if (initialStatus.PhoneNumberCurrentTokens < 5.0)
        {
            var waitTime = (int)Math.Ceiling((5.0 - initialStatus.PhoneNumberCurrentTokens) * 1000);
            _output.WriteLine($"Waiting {waitTime}ms for bucket to refill");
            await Task.Delay(waitTime);
        }
        
        // Act - Create multiple concurrent requests
        var tasks = new List<Task<RateLimitCheckResponse>>();
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(SendRateLimitRequest(phoneNumber));
        }
        
        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
        
        // Collect results
        var results = tasks.Select(t => t.Result.Allowed).ToList();
        
        // Assert - Should have exactly 5 allowed requests (matching phone bucket size)
        var allowedCount = results.Count(r => r);
        _output.WriteLine($"Allowed requests: {allowedCount} out of {taskCount}");
        Assert.Equal(5, allowedCount);
        
        // Final status check
        var finalStatus = await GetBucketStatus(phoneNumber);
        _output.WriteLine($"Final tokens: {finalStatus.PhoneNumberCurrentTokens}");
        Assert.True(finalStatus.PhoneNumberCurrentTokens < 1.0);
    }

    [Fact]
    public async Task RequestsPerSecond_MeasureBenchmark()
    {
        // Arrange
        var phoneNumber = "+19255551212";
        var duration = TimeSpan.FromSeconds(5);
        var requestDelay = TimeSpan.FromMilliseconds(100); // 10 requests per second
        
        _output.WriteLine($"Testing throughput over {duration.TotalSeconds} seconds with one request every {requestDelay.TotalMilliseconds}ms");
        
        // Reset bucket status first
        await GetAndResetBucketStatus(phoneNumber);
        await GetAndResetGlobalBucket(phoneNumber);
        
        // Act - Run requests for the specified duration
        var startTime = DateTime.UtcNow;
        var endTime = startTime + duration;
        
        var requests = 0;
        var accepted = 0;
        var rejected = 0;
        
        // Run until the time is up
        while (DateTime.UtcNow < endTime)
        {
            var response = await _client.PostAsync("/api/RateLimiter/check", 
                JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber }));
            var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
            
            requests++;
            
            if (result.Allowed)
                accepted++;
            else
                rejected++;
            
            // Only delay if we're not past the end time
            if (DateTime.UtcNow + requestDelay < endTime)
                await Task.Delay(requestDelay);
        }
        
        // Get elapsed time
        var elapsed = DateTime.UtcNow - startTime;
        
        // Calculate metrics
        var requestsPerSecond = requests / elapsed.TotalSeconds;
        var acceptedPerSecond = accepted / elapsed.TotalSeconds;
        var rejectedPerSecond = rejected / elapsed.TotalSeconds;
        
        // Output results
        _output.WriteLine($"Test ran for {elapsed.TotalSeconds:F2} seconds");
        _output.WriteLine($"Total requests: {requests}");
        _output.WriteLine($"Accepted: {accepted} ({acceptedPerSecond:F2}/sec)");
        _output.WriteLine($"Rejected: {rejected} ({rejectedPerSecond:F2}/sec)");
        _output.WriteLine($"Overall throughput: {requestsPerSecond:F2} requests/sec");
        
        // Assert - We expect roughly the configured rate
        Assert.True(requests > 0, "Should have processed at least one request");
        Assert.InRange(acceptedPerSecond, 0.5, 2.0); // Roughly matches our 1/sec phone number limit
    }

    [Fact]
    public async Task BucketCleanup_OldBucketsRemoved()
    {
        // This test requires access to provider internals, so we'll get them from DI
        var provider = _factory.Services.GetRequiredService<ITokenBucketProvider>() as LocalTokenBucketProvider;
        Assert.NotNull(provider);
        
        // Generate a unique phone number
        var phoneNumber = $"+1555{DateTime.Now.ToString("HHmmss")}";
        
        _output.WriteLine($"Testing bucket cleanup with phone number: {phoneNumber}");
        
        // Make a request to create the bucket
        await _client.PostAsync("/api/RateLimiter/check", 
            JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber }));
        
        // Verify bucket exists
        var bucketField = provider.GetType().GetField("_phoneNumberBuckets", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var buckets = (System.Collections.Concurrent.ConcurrentDictionary<string, SMSRateLimiter.Core.RateLimiting.TokenBucket>)
            bucketField.GetValue(provider);
        
        Assert.True(buckets.ContainsKey(phoneNumber), "Bucket should exist after request");
        
        // Get the bucket
        var bucket = provider.GetPhoneNumberBucket(phoneNumber);
        _output.WriteLine($"Bucket created at: {bucket.LastUsed}");
        var config = _factory.Services.GetRequiredService<RateLimiterConfig>();
        var cleanupTime = DateTimeOffset.UtcNow.AddMilliseconds(-config.InactiveBucketTimeoutMilliSec);
        _output.WriteLine($"Cleanup set to remove buckets older than: {cleanupTime}");
        // Set LastUsed to a time in the past (via reflection since it's read-only by design)
        var prop = bucket.GetType().GetProperty("LastUsed");
        var setter = prop.GetSetMethod(true); // Get non-public setter
        setter.Invoke(bucket, new object[] { DateTimeOffset.UtcNow.AddSeconds(-20) });
        
        _output.WriteLine("Set bucket LastUsed to 20 seconds ago");
        _output.WriteLine($"Current LastUsed: {bucket.LastUsed}");
        _output.WriteLine("Waiting for cleanup cycle...");
        
        // Wait for cleanup (configured at 5 seconds interval and 10 second timeout)
        await Task.Delay(11000); // Wait slightly longer than the cleanup interval + timeout
        
        // Check if bucket was removed
        Assert.False(buckets.ContainsKey(phoneNumber), "Bucket should be removed after timeout");
        _output.WriteLine("Bucket was successfully removed by cleanup process");
    }

    private async Task<BucketStatusResponse> GetBucketStatus(string phoneNumber)
    {
        var response = await _client.GetAsync($"/api/RateLimiter/status/{phoneNumber}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BucketStatusResponse>();
    }

    private async Task<RateLimitCheckResponse> SendRateLimitRequest(string phoneNumber)
    {
        var requestContent = JsonContent.Create(new RateLimitCheckRequest { PhoneNumber = phoneNumber });
        var response = await _client.PostAsync("/api/RateLimiter/check", requestContent);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>();
    }

    private async Task GetAndResetBucketStatus(string phoneNumber)
    {
        // Get current status
        var status = await GetBucketStatus(phoneNumber);
        
        // If bucket is not full, wait for refill
        if (status.PhoneNumberCurrentTokens < status.PhoneNumberMaxTokens)
        {
            var tokensNeeded = status.PhoneNumberMaxTokens - status.PhoneNumberCurrentTokens;
            var secondsToWait = Math.Ceiling(tokensNeeded / status.PhoneNumberRefillRate);
            _output.WriteLine($"Waiting {secondsToWait} seconds for phone bucket to refill");
            await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
        }
    }

    private async Task GetAndResetGlobalBucket(string phoneNumber)
    {
        // Get current status
        var status = await GetBucketStatus(phoneNumber);
        
        // If bucket is not full, wait for refill
        if (status.GlobalCurrentTokens < status.GlobalMaxTokens)
        {
            var tokensNeeded = status.GlobalMaxTokens - status.GlobalCurrentTokens;
            var secondsToWait = Math.Ceiling(tokensNeeded / status.GlobalRefillRate);
            _output.WriteLine($"Waiting {secondsToWait} seconds for global bucket to refill");
            await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
        }
    }
}