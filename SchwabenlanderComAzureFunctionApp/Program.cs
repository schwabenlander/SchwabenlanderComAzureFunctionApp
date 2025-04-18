using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(s =>
{
    var connectionString = builder.Configuration.GetConnectionString("CommService")!;
    return new EmailClient(connectionString);
});

builder.Services.AddHttpClient();

builder.Build().Run();