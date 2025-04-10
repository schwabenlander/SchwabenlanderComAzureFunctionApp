using System.Runtime.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SchwabenlanderComAzureFunctionApp;

public class SaveToDatabase
{
    private readonly ILogger<SaveToDatabase> _logger;

    public SaveToDatabase(ILogger<SaveToDatabase> logger)
    {
        _logger = logger;
    }

    [Function(nameof(SaveToDatabase))]
    public async Task Run(
        [ServiceBusTrigger("messages", "cosmosdbsubscription", Connection = "ServiceBusConnection", IsBatched = false)]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {Id}", message.MessageId);
        _logger.LogInformation("Message Body: {Body}", message.Body);

        try
        {
            var deserializedMessage = message.Body.ToObjectFromJson<ContactFormMessage>();

            if (deserializedMessage is null)
            {
                throw new SerializationException("Failed to deserialize message");
            }
            
            using var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnection"));
            var container = cosmosClient.GetContainer("schwabenlander-com-messagedb", "messages"); 
            
            // Insert item into Cosmos DB
            await container.CreateItemAsync(new
            {
                id = deserializedMessage.Id,
                name = deserializedMessage.Name,
                email = deserializedMessage.Email,
                phone = deserializedMessage.Phone,
                messageId = deserializedMessage.Message,
                timestamp = deserializedMessage.Timestamp
            }, new PartitionKey(deserializedMessage.Email));
            
            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to database");
        }
    }
}