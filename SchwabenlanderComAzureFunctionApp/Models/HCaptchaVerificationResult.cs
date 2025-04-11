using System.Text.Json.Serialization;

namespace SchwabenlanderComAzureFunctionApp.Models;

public class HCaptchaVerificationResult
{
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }
    
    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }
    
    [JsonPropertyName("hostname")]
    public string? HostName { get; set; }
}