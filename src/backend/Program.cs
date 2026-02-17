using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
builder.Services.AddHttpClient<IUniFiService, UniFiService>();

builder.Build().Run();
