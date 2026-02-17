using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UniFiCameraControl.Models;
using UniFiCameraControl.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure UniFi settings
builder.Services.Configure<UniFiConfiguration>(
    builder.Configuration.GetSection("UniFi"));

// Configure Azure Table Storage (Azurite for local dev)
var storageConnectionString = builder.Configuration.GetValue<string>("AzureWebJobsStorage") 
    ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new TableServiceClient(storageConnectionString));

// Register services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Configure HttpClient with SSL handling for UniFi
// Register as Singleton to maintain authentication cookie state across requests
builder.Services.AddHttpClient<IUniFiService, UniFiService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var config = builder.Configuration.GetSection("UniFi");
        var ignoreSsl = config.GetValue<bool>("IgnoreSslErrors");
        
        // WARNING: Disabling SSL validation is insecure and should only be used for development
        // with self-signed certificates. In production, use proper certificate validation or
        // install the UniFi certificate in the trusted store.
        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, errors) => true;
        }
        
        return handler;
    });

// Override default transient lifetime to singleton for auth cookie persistence
builder.Services.AddSingleton<IUniFiService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(typeof(IUniFiService).FullName ?? "IUniFiService");
    var config = sp.GetRequiredService<IOptions<UniFiConfiguration>>();
    var logger = sp.GetRequiredService<ILogger<UniFiService>>();
    var configService = sp.GetRequiredService<IConfigurationService>();
    return new UniFiService(httpClient, config, logger, configService);
});

builder.Build().Run();
