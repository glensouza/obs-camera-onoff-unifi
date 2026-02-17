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
IConfigurationSection uniFiSection = builder.Configuration.GetSection("UniFi");
UniFiConfiguration? uniFiConfig = uniFiSection.Get<UniFiConfiguration>();
if (uniFiConfig == null || string.IsNullOrWhiteSpace(uniFiConfig.Host))
{
    throw new InvalidOperationException("Missing configuration: 'UniFi:Host' must be set.");
}

builder.Services.Configure<UniFiConfiguration>(uniFiSection);

// Configure HttpClient with SSL handling for UniFi
string uniFiHttpClientName = "UniFiService";
builder.Services.AddHttpClient(uniFiHttpClientName)
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        HttpClientHandler handler = new();
        IConfigurationSection config = builder.Configuration.GetSection("UniFi");
        // In development treat SSL validation as optional to allow self-signed UniFi controller certs
        bool ignoreSsl = config.GetValue<bool>("IgnoreSslErrors") || builder.Environment.IsDevelopment();

        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });

// Register as singleton for auth cookie persistence
builder.Services.AddSingleton<UniFiService>(sp =>
{
    IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    HttpClient httpClient = httpClientFactory.CreateClient(uniFiHttpClientName);
    IOptions<UniFiConfiguration> config = sp.GetRequiredService<IOptions<UniFiConfiguration>>();
    ILogger<UniFiService> logger = sp.GetRequiredService<ILogger<UniFiService>>();
    return new UniFiService(httpClient, config, logger);
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

