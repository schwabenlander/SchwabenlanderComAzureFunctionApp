using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SchwabenlanderComAzureFunctionApp;

public class SendMessage
{
    private readonly ILogger<SendMessage> _logger;
    private readonly IConfiguration _configuration;

    public SendMessage(ILogger<SendMessage> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("PublishMessage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var formData = await req.ReadFromJsonAsync<ContactFormMessage>();

        if (formData is null || 
            string.IsNullOrEmpty(formData.Name) || 
            string.IsNullOrEmpty(formData.Email) || 
            string.IsNullOrEmpty(formData.Message))
        {
            throw new ArgumentException(message: "Form missing missing one or more required values.", paramName: nameof(req));
        }
        
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

}