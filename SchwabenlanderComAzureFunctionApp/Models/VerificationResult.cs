using System.Text.Json.Serialization;

namespace SchwabenlanderComAzureFunctionApp.Models;

public class VerificationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTimestamp { get; set; }
    
    [JsonPropertyName("hostname")]
    public string? HostName { get; set; }
}