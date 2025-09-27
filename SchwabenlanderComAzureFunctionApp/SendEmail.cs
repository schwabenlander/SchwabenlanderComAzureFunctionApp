using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SchwabenlanderComAzureFunctionApp.Models;

namespace SchwabenlanderComAzureFunctionApp;

public class SendEmail(EmailClient emailClient, ILogger<SendEmail> logger)
{
    [Function(nameof(SendEmail))]
    public async Task Run(
        [ServiceBusTrigger("messages", "commservicesubscription", Connection = "ServiceBusConnection", IsBatched = false)]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Message ID: {Id}", message.MessageId);
        logger.LogInformation("Message Body: {Body}", message.Body);

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
                                 
                                 IP Address: {formMessage?.IpAddress}
                                 Timestamp: {formMessage?.Timestamp}
                                 Message ID: {formMessage?.Id}
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
            logger.LogError(ex, "Error sending message");
        }
        
        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}