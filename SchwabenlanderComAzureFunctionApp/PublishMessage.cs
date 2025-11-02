using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchwabenlanderComAzureFunctionApp.Models;

namespace SchwabenlanderComAzureFunctionApp;

public class PublishMessage(HttpClient httpClient, ServiceBusClient serviceBusClient, ILogger<PublishMessage> logger)
{
    /// <summary>
    /// Azure Function to handle HTTP POST requests and publish messages to Azure Service Bus.
    /// </summary>
    /// <param name="req">The HTTP request containing the form data.</param>
    /// <returns>An IActionResult indicating the result of the operation.</returns>
    [Function("PublishMessage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequest req)
    {
        try
        {
            // Deserialize form data
            if (!req.ContentType?.Contains("application/json") ?? true)
            {
                return new BadRequestObjectResult("Invalid content type. Expected 'application/json'.");
            }
            
            var formData = await req.ReadFromJsonAsync<ContactFormMessage>();
            
            if (formData is null)
            {
                return new BadRequestObjectResult("Unable to read contact form message.");
            }
            
            logger.LogInformation("Received contact form data: {FormData}", JsonSerializer.Serialize(formData));

            ValidateFormData(formData);

            // Verify hCaptcha
            if (!await VerifyHCaptchaAsync(formData.HcaptchaToken))
            {
                return new BadRequestObjectResult("CAPTCHA validation failed.");
            }
            
            logger.LogInformation("Publishing message to Azure Message Bus with ID: {Id}", formData.Id);
            
            await PublishToServiceBusAsync(new
            {
                name = formData.Name,
                email = formData.Email,
                phone = formData.Phone,
                message = formData.Message,
                
                // Set metadata values
                id = Guid.NewGuid(),
                timestamp = DateTimeOffset.UtcNow,
                ipaddress = formData.IpAddress
            });
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message: {Message}", ex.Message);
            return new BadRequestObjectResult("Error publishing message.");
        }
    }
    
    /// <summary>
    /// Validates the form data to ensure all required fields are present and not empty.
    /// </summary>
    /// <param name="formData">The form data to validate.</param>
    /// <exception cref="ArgumentException">Thrown if any required field is missing or empty.</exception>
    private static void ValidateFormData(ContactFormMessage formData)
    {
        if (formData is null ||
            string.IsNullOrEmpty(formData.Name) ||
            string.IsNullOrEmpty(formData.Email) ||
            string.IsNullOrEmpty(formData.Message) ||
            string.IsNullOrEmpty(formData.HcaptchaToken))
        {
            throw new ArgumentException("Form is missing one or more required values.");
        }
    }
    
    /// <summary>
    /// Verifies the hCaptcha token by sending a request to the hCaptcha verification endpoint.
    /// </summary>
    /// <param name="hCaptchaToken">The hCaptcha token to verify.</param>
    /// <returns>A boolean indicating whether the hCaptcha verification was successful.</returns>
    private async Task<bool> VerifyHCaptchaAsync(string? hCaptchaToken)
    {
        try
        {
            var secretKey = Utilities.GetEnvironmentVariable("HCaptchaSecretKey")!;
            var verificationResponse = await httpClient.PostAsync(
                Utilities.GetEnvironmentVariable("HCaptchaVerificationUrl"),
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", hCaptchaToken)
                }));

            if (!verificationResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("hCaptcha verification failed with status code: {StatusCode}", verificationResponse.StatusCode);
                return false;
            }

            var verificationResult = JsonSerializer.Deserialize<HCaptchaVerificationResult>(
                await verificationResponse.Content.ReadAsStringAsync());

            return verificationResult is not null && verificationResult.IsSuccess;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during hCaptcha verification");
            return false;
        }
    }
    
    /// <summary>
    /// Publishes a message to the Azure Service Bus topic.
    /// </summary>
    /// <param name="message">The message object to serialize and publish.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task PublishToServiceBusAsync(object message)
    {
        var serviceBusSender = serviceBusClient.CreateSender(Environment.GetEnvironmentVariable("TOPIC_NAME"));
        var serializedMessage = JsonSerializer.Serialize(message);

        var serviceBusMessage = new ServiceBusMessage(serializedMessage);

        await serviceBusSender.SendMessageAsync(serviceBusMessage);
        await serviceBusSender.DisposeAsync();
    }
}