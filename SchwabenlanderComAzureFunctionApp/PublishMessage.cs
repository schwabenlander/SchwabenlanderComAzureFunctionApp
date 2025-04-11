using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SchwabenlanderComAzureFunctionApp;

public class PublishMessage
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PublishMessage> _logger;

    public PublishMessage(HttpClient httpClient, ILogger<PublishMessage> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    [Function("PublishMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            // Deserialize form data
            var formData = await req.ReadFromJsonAsync<ContactFormMessage>();

            if (formData is null || 
                string.IsNullOrEmpty(formData.Name) || 
                string.IsNullOrEmpty(formData.Email) || 
                string.IsNullOrEmpty(formData.Message) ||
                string.IsNullOrEmpty(formData.HcaptchaToken))
            {
                throw new ArgumentException(message: "Form missing missing one or more required values.", paramName: nameof(req));
            }

            // Verify hCaptcha
            if (!await VerifyHcaptchaAsync(formData.HcaptchaToken))
            {
                return new BadRequestObjectResult("CAPTCHA validation failed.");
            }
            
            _logger.LogInformation("Publishing message to Azure Message Bus");
            await PublishToServiceBusAsync(new
            {
                name = formData.Name,
                email = formData.Email,
                phone = formData.Phone,
                message = formData.Message,
                // Set metadata values
                id = Guid.NewGuid(),
                timestamp = DateTimeOffset.UtcNow
            });
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message: {Message}", ex.Message);
            return new BadRequestObjectResult("Error publishing message.");
        }
    }
    
    private async Task<bool> VerifyHcaptchaAsync(string hCaptchaToken)
    {
        var secretKey = Environment.GetEnvironmentVariable("HCaptchaSecretKey")!;
        var verificationResponse = await _httpClient.PostAsync(
            Environment.GetEnvironmentVariable("HCaptchaVerificationUrl"),
            new FormUrlEncodedContent([
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", hCaptchaToken)
            ]));

        var verificationResult = JsonConvert.DeserializeObject<dynamic>(
            await verificationResponse.Content.ReadAsStringAsync());

        return verificationResult is not null && (bool)verificationResult.success;
    }
    
    private static async Task PublishToServiceBusAsync(object message)
    {
        var serviceBusClient = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnection"));
        var serviceBusSender = serviceBusClient.CreateSender(Environment.GetEnvironmentVariable("TOPIC_NAME"));
        var serializedMessage = JsonSerializer.Serialize(message);

        var serviceBusMessage = new ServiceBusMessage(serializedMessage);

        await serviceBusSender.SendMessageAsync(serviceBusMessage);
        await serviceBusSender.DisposeAsync();
        await serviceBusClient.DisposeAsync();
    }
}