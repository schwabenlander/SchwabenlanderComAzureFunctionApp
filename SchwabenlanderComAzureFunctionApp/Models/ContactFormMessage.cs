using System.Text.Json.Serialization;

namespace SchwabenlanderComAzureFunctionApp.Models;

public class ContactFormMessage
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("email")]
    public required string Email { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("message")]
    public required string Message { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("h-captcha-response")]
    public string? HcaptchaToken { get; set; }
    
    [JsonPropertyName("ipaddress")]
    public string? IpAddress { get; set; }
}