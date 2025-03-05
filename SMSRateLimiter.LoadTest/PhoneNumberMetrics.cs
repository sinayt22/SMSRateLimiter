/// <summary>
/// Metrics for a specific phone number
/// </summary>
public class PhoneNumberMetrics
{
    public string PhoneNumber { get; }
    public int Requests { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    
    public PhoneNumberMetrics(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
    }
}
