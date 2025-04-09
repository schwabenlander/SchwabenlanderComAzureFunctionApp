using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SchwabenlanderComAzureFunctionApp;

public class SendEmail
{
    private readonly ILogger<SendEmail> _logger;
    private readonly IConfiguration _configuration;

    public SendEmail(ILogger<SendEmail> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function(nameof(SendEmail))]
    public async Task Run(
        [ServiceBusTrigger("messages", "commservicesubscription", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {Id}", message.MessageId);
        _logger.LogInformation("Message Body: {Body}", message.Body);

        try
        {
            var connectionString = _configuration.GetConnectionString("CommService");
            var emailClient = new EmailClient(connectionString);

            var formMessage = message.Body.ToObjectFromJson<ContactFormMessage>();
            
            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@69119a3d-48e7-40c4-9af7-1911d2759147.azurecomm.net",
                content: new EmailContent("New Message from Schwabenlander.com")
                {
                    PlainText = $"""
                                 You received a new message from Schwabenlander.com!

                                 Name: {formMessage?.Name}
                                 Email: {formMessage?.Email}
                                 Phone: {formMessage?.Phone}
                                 Message: {formMessage?.Message}
                                 """
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new (Environment.GetEnvironmentVariable("DESTINATION_ADDRESS"))
                }));
            
            await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
        }
        
        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}