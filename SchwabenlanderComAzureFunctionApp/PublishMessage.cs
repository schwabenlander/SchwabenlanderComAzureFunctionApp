using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SchwabenlanderComAzureFunctionApp;

public class PublishMessage
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PublishMessage> _logger;

    public PublishMessage(IConfiguration configuration,ILogger<PublishMessage> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [Function("PublishMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var formData = await req.ReadFromJsonAsync<ContactFormMessage>();

            if (formData is null || 
                string.IsNullOrEmpty(formData.Name) || 
                string.IsNullOrEmpty(formData.Email) || 
                string.IsNullOrEmpty(formData.Message))
            {
                throw new ArgumentException(message: "Form missing missing one or more required values.", paramName: nameof(req));
            }
            
            formData.MessageTimeStamp = DateTimeOffset.UtcNow;
            formData.id = Guid.NewGuid();
            
            _logger.LogInformation("Publishing message to Azure Message Bus");

            var serviceBusClient = new ServiceBusClient(_configuration.GetConnectionString("ServiceBus"));
            var serviceBusSender = serviceBusClient.CreateSender(Environment.GetEnvironmentVariable("TOPIC_NAME"));
            var serializedMessage = JsonSerializer.Serialize(formData);
            
            var serviceBusMessage = new ServiceBusMessage(serializedMessage);

            await serviceBusSender.SendMessageAsync(serviceBusMessage);
            await serviceBusSender.DisposeAsync();
            await serviceBusClient.DisposeAsync();
            
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message: {Message}", ex.Message);
            return new BadRequestResult();
        }
    }

}