using Newtonsoft.Json;

namespace SchwabenlanderComAzureFunctionApp;

public class ContactFormMessage
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    
    [JsonProperty("h-captcha-response")]
    public string? HcaptchaToken { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("email")]
    public string? Email { get; set; }
    
    [JsonProperty("phone")]
    public string? Phone { get; set; }
    
    [JsonProperty("message")]
    public string? Message { get; set; }
}