using Newtonsoft.Json;

namespace SMSRateLimiter.Tests.LoadTest;

public class Program
{
    private static readonly string BaseUrl = "http://localhost:5139";
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("SMS Rate Limiter Load Test");
        Console.WriteLine("==========================");
        
        var loadTest = new RateLimiterLoadTest(BaseUrl);
        var results = new List<LoadTestResults>();
        
        // Run all scenarios
        results.Add(await RunSteadyLoadScenario(loadTest));
        results.Add(await RunBurstLoadScenario(loadTest));
        results.Add(await RunHotspotScenario(loadTest));
        
        // Save results to files
        SaveResults(results);
        
        Console.WriteLine("\nAll tests completed.");
    }
    
    private static async Task<LoadTestResults> RunSteadyLoadScenario(RateLimiterLoadTest loadTest)
    {
        Console.WriteLine("\nRunning steady load scenario...");
        
        var scenario = new LoadTestScenario
        {
            Name = "Steady Load",
            PhoneNumbers = GeneratePhoneNumbers(50),
            RequestsPerSecond = 10,
            Distribution = PhoneNumberDistribution.Sequential
        };
        
        var results = await loadTest.RunScenarioAsync(scenario, TimeSpan.FromSeconds(30));
        Console.WriteLine(results);
        
        return results;
    }
    
    private static async Task<LoadTestResults> RunBurstLoadScenario(RateLimiterLoadTest loadTest)
    {
        Console.WriteLine("\nRunning burst load scenario...");
        
        var scenario = new LoadTestScenario
        {
            Name = "Burst Load",
            PhoneNumbers = GeneratePhoneNumbers(10),
            RequestsPerSecond = 20, // Higher than system capacity
            Distribution = PhoneNumberDistribution.Sequential
        };
        
        var results = await loadTest.RunScenarioAsync(scenario, TimeSpan.FromSeconds(20));
        Console.WriteLine(results);
        
        return results;
    }
    
    private static async Task<LoadTestResults> RunHotspotScenario(RateLimiterLoadTest loadTest)
    {
        Console.WriteLine("\nRunning hotspot scenario...");
        
        var scenario = new LoadTestScenario
        {
            Name = "Hotspot Pattern",
            PhoneNumbers = GeneratePhoneNumbers(30),
            RequestsPerSecond = 15,
            Distribution = PhoneNumberDistribution.WeightedRandom
        };
        
        var results = await loadTest.RunScenarioAsync(scenario, TimeSpan.FromSeconds(25));
        Console.WriteLine(results);
        
        return results;
    }
    
    private static string[] GeneratePhoneNumbers(int count)
    {
        var numbers = new string[count];
        for (int i = 0; i < count; i++)
        {
            numbers[i] = $"+1555{i:D7}";
        }
        return numbers;
    }
    
    private static void SaveResults(List<LoadTestResults> results)
    {
        // Create results directory if it doesn't exist
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "LoadTestResults");
        Directory.CreateDirectory(resultsDir);
        
        // Save summary report
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var summaryPath = Path.Combine(resultsDir, $"summary_{timestamp}.txt");
        
        using (var writer = new StreamWriter(summaryPath))
        {
            writer.WriteLine("SMS Rate Limiter Load Test Summary");
            writer.WriteLine("==================================");
            writer.WriteLine($"Time: {DateTime.Now}");
            writer.WriteLine();
            
            foreach (var result in results)
            {
                writer.WriteLine(result);
                writer.WriteLine();
            }
        }
        
        Console.WriteLine($"Summary report saved to: {summaryPath}");
        
        // Save detailed JSON for each result
        foreach (var result in results)
        {
            var jsonPath = Path.Combine(resultsDir, $"{result.ScenarioName.Replace(" ", "_").ToLower()}_{timestamp}.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(result, Formatting.Indented));
            Console.WriteLine($"Detailed results saved to: {jsonPath}");
        }
        
        // Generate CSV file for time series data
        var csvPath = Path.Combine(resultsDir, $"timeseries_{timestamp}.csv");
        using (var writer = new StreamWriter(csvPath))
        {
            writer.WriteLine("Scenario,Timestamp,RequestRate,AcceptedRate,RejectedRate");
            
            foreach (var result in results)
            {
                var timepoints = result.RequestsPerSecond.Keys
                    .OrderBy(t => t)
                    .ToList();
                
                foreach (var timepoint in timepoints)
                {
                    var requests = result.RequestsPerSecond.TryGetValue(timepoint, out var r) ? r : 0;
                    var accepted = result.AcceptedPerSecond.TryGetValue(timepoint, out var a) ? a : 0;
                    var rejected = result.RejectedPerSecond.TryGetValue(timepoint, out var d) ? d : 0;
                    
                    writer.WriteLine($"{result.ScenarioName},{timepoint:yyyy-MM-dd HH:mm:ss},{requests},{accepted},{rejected}");
                }
            }
        }
        
        Console.WriteLine($"Time series data saved to: {csvPath}");
    }
}