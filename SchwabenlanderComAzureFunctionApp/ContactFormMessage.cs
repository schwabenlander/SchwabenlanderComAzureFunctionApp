using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SchwabenlanderComAzureFunctionApp;

public class ContactFormMessage
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonProperty("email")]
    [JsonPropertyName("email")]
    public required string Email { get; set; }
    
    [JsonProperty("phone")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonProperty("message")]
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonProperty("timestamp")]
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    
    [JsonProperty("h-captcha-response")]
    [JsonPropertyName("h-captcha-response")]
    public string? HcaptchaToken { get; set; }
}