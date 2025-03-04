namespace SMSRateLimiter.API.Models;

public class ErrorResponse
{
    public required string Message { get; set; }
    public string? Code { get; set; }
    public IEnumerable<string>? Details { get; set; }
}