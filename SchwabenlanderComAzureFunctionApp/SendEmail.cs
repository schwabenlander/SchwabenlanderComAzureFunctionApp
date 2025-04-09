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
    private readonly EmailClient _emailClient;

    public SendEmail(EmailClient emailClient, ILogger<SendEmail> logger)
    {
        _emailClient = emailClient;
        _logger = logger;
    }

    [Function(nameof(SendEmail))]
    public async Task Run(
        [ServiceBusTrigger("messages", "commservicesubscription", Connection = "ServiceBusConnection", IsBatched = false)]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {Id}", message.MessageId);
        _logger.LogInformation("Message Body: {Body}", message.Body);

        try
        {
            var formMessage = message.Body.ToObjectFromJson<ContactFormMessage>();
            
            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@69119a3d-48e7-40c4-9af7-1911d2759147.azurecomm.net",
                content: new EmailContent("New Message from Schwabenlander.com")
                {
                    PlainText = $"""
                                 You received a new message from Schwabenlander.com!
                                 
                                 ------------------------------------------
                                 Name: {formMessage?.Name}
                                 Email: {formMessage?.Email}
                                 Phone: {formMessage?.Phone}
                                 Message: {formMessage?.Message}
                                 ------------------------------------------
                                 
                                 Timestamp: {formMessage?.MessageTimeStamp}
                                 Message ID: {formMessage?.id}
                                 """
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new (Environment.GetEnvironmentVariable("DESTINATION_ADDRESS"))
                }));
            
            await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
        }
        
        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}