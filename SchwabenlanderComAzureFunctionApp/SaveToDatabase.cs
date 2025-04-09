using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SchwabenlanderComAzureFunctionApp;

public class SaveToDatabase
{
    private readonly ILogger<SaveToDatabase> _logger;
    private readonly CosmosClient _cosmosClient;

    public SaveToDatabase(CosmosClient cosmosClient, ILogger<SaveToDatabase> logger)
    {
        _cosmosClient = cosmosClient;
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
            
            var container = _cosmosClient.GetContainer("schwabenlander-com-messagedb", "messages"); 
            
            // Insert item into Cosmos DB
            await container.CreateItemAsync(deserializedMessage, new PartitionKey(deserializedMessage.Email));
            
            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to database");
        }
    }
}