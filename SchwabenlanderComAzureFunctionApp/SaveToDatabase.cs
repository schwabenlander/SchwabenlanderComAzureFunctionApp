using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SchwabenlanderComAzureFunctionApp.Models;

namespace SchwabenlanderComAzureFunctionApp;

public class SaveToDatabase(ILogger<SaveToDatabase> logger)
{
    [Function(nameof(SaveToDatabase))]
    public async Task Run(
        [ServiceBusTrigger("messages", "cosmosdbsubscription", Connection = "ServiceBusConnection", IsBatched = false)]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        logger.LogInformation("Message ID: {Id}", message.MessageId);
        logger.LogInformation("Message Body: {Body}", message.Body);

        try
        {
            var deserializedMessage = message.Body.ToObjectFromJson<ContactFormMessage>();

            if (deserializedMessage is null)
            {
                throw new SerializationException("Failed to deserialize message");
            }
            
            var cosmosClientOptions = new CosmosClientOptions()
            {
                UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };
            using var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDbConnection"), cosmosClientOptions);
            var container = cosmosClient.GetContainer("schwabenlander-com-messagedb", "messages");
            
            // Insert item into Cosmos DB
            await container.CreateItemAsync(deserializedMessage, new PartitionKey(deserializedMessage.Id.ToString()));
            
            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving message to database");
        }
    }
}