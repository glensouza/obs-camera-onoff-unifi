using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using OBSCameraPowerControl.Components;
using OBSCameraPowerControl.Models;
using OBSCameraPowerControl.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Razor components with interactive server rendering
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure UniFi settings
builder.Services.Configure<UniFiConfiguration>(builder.Configuration.GetSection("UniFi"));

// Configure Azure Table Storage (Azurite for local dev)
string storageConnectionString = builder.Configuration.GetValue<string>("AzureStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new TableServiceClient(storageConnectionString));

// Register services
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Configure HttpClient with SSL handling for UniFi
builder.Services.AddHttpClient<IUniFiService, UniFiService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        HttpClientHandler handler = new();
        IConfigurationSection config = builder.Configuration.GetSection("UniFi");
        bool ignoreSsl = config.GetValue<bool>("IgnoreSslErrors");

        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        return handler;
    });

// Override default transient lifetime to singleton for auth cookie persistence
builder.Services.AddSingleton<IUniFiService>(sp =>
{
    IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    HttpClient httpClient = httpClientFactory.CreateClient(typeof(IUniFiService).FullName ?? "IUniFiService");
    IOptions<UniFiConfiguration> config = sp.GetRequiredService<IOptions<UniFiConfiguration>>();
    ILogger<UniFiService> logger = sp.GetRequiredService<ILogger<UniFiService>>();
    IConfigurationService configService = sp.GetRequiredService<IConfigurationService>();
    return new UniFiService(httpClient, config, logger, configService);
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
