using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Functions Web Application
builder.ConfigureFunctionsWebApplication();

// Centralized configuration for ServiceBusClient
builder.Services.AddSingleton<ServiceBusClient>(s =>
{
    var connectionString = builder.Configuration.GetValue<string>("ServiceBusConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("ServiceBusConnection is not configured.");
    }
    return new ServiceBusClient(connectionString);
});

// Centralized configuration for EmailClient
builder.Services.AddSingleton<EmailClient>(s =>
{
    var connectionString = builder.Configuration.GetConnectionString("CommService");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("CommService connection string is not configured.");
    }
    return new EmailClient(connectionString);
});

// Register HttpClient
builder.Services.AddHttpClient();

// Build and run the application
builder.Build().Run();