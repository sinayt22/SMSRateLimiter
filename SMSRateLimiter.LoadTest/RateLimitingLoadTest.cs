using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using SMSRateLimiter.API.Models;

namespace SMSRateLimiter.Tests.LoadTest;

/// <summary>
/// A load testing utility for the SMS Rate Limiter service
/// </summary>
public class RateLimiterLoadTest
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly ConcurrentDictionary<string, PhoneNumberMetrics> _phoneMetrics = new();
    private readonly ConcurrentDictionary<DateTimeOffset, int> _globalRequestsPerSecond = new();
    private readonly ConcurrentDictionary<DateTimeOffset, int> _globalAcceptedPerSecond = new();
    private readonly ConcurrentDictionary<DateTimeOffset, int> _globalRejectedPerSecond = new();
    
    public RateLimiterLoadTest(string baseUrl)
    {
        _baseUrl = baseUrl;
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Run a load test scenario
    /// </summary>
    /// <param name="scenario">The scenario to run</param>
    /// <param name="duration">How long to run the test</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    public async Task<LoadTestResults> RunScenarioAsync(
        LoadTestScenario scenario, 
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Starting load test scenario: {scenario.Name}");
        Console.WriteLine($"Duration: {duration.TotalSeconds} seconds");
        Console.WriteLine($"Phone number count: {scenario.PhoneNumbers.Length}");
        Console.WriteLine($"Request rate: {scenario.RequestsPerSecond} req/sec");
        
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime + duration;
        var stopwatch = Stopwatch.StartNew();
        
        // Reset metrics
        _phoneMetrics.Clear();
        _globalRequestsPerSecond.Clear();
        _globalAcceptedPerSecond.Clear();
        _globalRejectedPerSecond.Clear();
        
        // Start the load test
        var requestTasks = new List<Task>();
        var timeSlice = TimeSpan.FromMilliseconds(1000.0 / scenario.RequestsPerSecond);
        
        while (DateTimeOffset.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            var requestStart = DateTimeOffset.UtcNow;
            var currentSecond = new DateTimeOffset(
                requestStart.Year, requestStart.Month, requestStart.Day,
                requestStart.Hour, requestStart.Minute, requestStart.Second, 
                requestStart.Offset);
            
            // Select a phone number based on the distribution strategy
            string phoneNumber = scenario.GetNextPhoneNumber();
            
            // Record the attempt
            _globalRequestsPerSecond.AddOrUpdate(
                currentSecond, 
                1, 
                (_, count) => count + 1);
            
            // Send a request
            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await _client.PostAsJsonAsync(
                        "/api/RateLimiter/check", 
                        new RateLimitCheckRequest { PhoneNumber = phoneNumber },
                        cancellationToken);
                    
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<RateLimitCheckResponse>(
                        cancellationToken: cancellationToken);
                    
                    // Update phone metrics
                    _phoneMetrics.AddOrUpdate(
                        phoneNumber,
                        _ => new PhoneNumberMetrics(phoneNumber) { 
                            Requests = 1, 
                            Accepted = result.Allowed ? 1 : 0,
                            Rejected = result.Allowed ? 0 : 1
                        },
                        (_, metrics) => {
                            metrics.Requests++;
                            if (result.Allowed)
                                metrics.Accepted++;
                            else
                                metrics.Rejected++;
                            return metrics;
                        });
                    
                    // Update global metrics
                    if (result.Allowed)
                    {
                        _globalAcceptedPerSecond.AddOrUpdate(
                            currentSecond, 1, (_, count) => count + 1);
                    }
                    else
                    {
                        _globalRejectedPerSecond.AddOrUpdate(
                            currentSecond, 1, (_, count) => count + 1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending request: {ex.Message}");
                }
            });
            
            requestTasks.Add(task);
            
            // Wait for next request time slice
            var elapsed = DateTimeOffset.UtcNow - requestStart;
            var remainingTimeSlice = timeSlice - elapsed;
            if (remainingTimeSlice > TimeSpan.Zero)
            {
                await Task.Delay(remainingTimeSlice, cancellationToken);
            }
        }
        
        // Wait for all pending requests to complete
        await Task.WhenAll(requestTasks);
        stopwatch.Stop();
        
        // Compile results
        var results = new LoadTestResults
        {
            ScenarioName = scenario.Name,
            Duration = stopwatch.Elapsed,
            TotalRequests = _phoneMetrics.Values.Sum(m => m.Requests),
            AcceptedRequests = _phoneMetrics.Values.Sum(m => m.Accepted),
            RejectedRequests = _phoneMetrics.Values.Sum(m => m.Rejected),
            PhoneMetrics = _phoneMetrics.Values.OrderByDescending(m => m.Requests).ToList(),
            RequestsPerSecond = _globalRequestsPerSecond.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value),
            AcceptedPerSecond = _globalAcceptedPerSecond.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value),
            RejectedPerSecond = _globalRejectedPerSecond.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value)
        };
        
        return results;
    }
}

