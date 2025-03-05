/// <summary>
/// Results from a load test run
/// </summary>
public class LoadTestResults
{
    public string ScenarioName { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public List<PhoneNumberMetrics> PhoneMetrics { get; set; }
    public Dictionary<DateTimeOffset, int> RequestsPerSecond { get; set; }
    public Dictionary<DateTimeOffset, int> AcceptedPerSecond { get; set; }
    public Dictionary<DateTimeOffset, int> RejectedPerSecond { get; set; }
    
    public double AcceptanceRate => TotalRequests > 0 
        ? (double)AcceptedRequests / TotalRequests 
        : 0;
    
    public double AverageRequestsPerSecond => Duration.TotalSeconds > 0 
        ? TotalRequests / Duration.TotalSeconds 
        : 0;
    
    public override string ToString()
    {
        return $@"Load Test Results: {ScenarioName}
Duration: {Duration.TotalSeconds:F2} seconds
Total Requests: {TotalRequests}
Accepted: {AcceptedRequests} ({AcceptanceRate:P2})
Rejected: {RejectedRequests} ({1 - AcceptanceRate:P2})
Avg Rate: {AverageRequestsPerSecond:F2} req/sec

Top 5 Phone Numbers:
{string.Join(Environment.NewLine, PhoneMetrics.Take(5).Select(m => $"  {m.PhoneNumber}: {m.Requests} requests, {m.Accepted} accepted ({(double)m.Accepted / m.Requests:P2})"))}
";
    }
}
