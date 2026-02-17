using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OBSCameraPowerControl.Models;

namespace OBSCameraPowerControl.Services;

public class UniFiService : IUniFiService
{
    private readonly HttpClient httpClient;
    private readonly UniFiConfiguration config;
    private readonly ILogger<UniFiService> logger;
    private readonly IConfigurationService configService;
    private readonly SemaphoreSlim authLock = new(1, 1);
    private string? authCookie;

    public UniFiService(HttpClient httpClient, IOptions<UniFiConfiguration> config, ILogger<UniFiService> logger, IConfigurationService configService)
    {
        this.httpClient = httpClient;
        this.config = config.Value;
        this.logger = logger;
        this.configService = configService;
        this.httpClient.BaseAddress = new Uri(this.config.Host.TrimEnd('/'));
    }

    private async Task<bool> AuthenticateAsync()
    {
        await this.authLock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(this.authCookie))
            {
                return true;
            }

            const string loginUrl = "/api/login";
            var loginData = new
            {
                username = this.config.Username,
                password = this.config.Password
            };

            StringContent content = new(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await this.httpClient.PostAsync(loginUrl, content);

            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
                {
                    this.authCookie = string.Join("; ", cookies);
                    this.logger.LogInformation("Successfully authenticated to UniFi controller");
                    return true;
                }
            }

            this.logger.LogError("Authentication failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error during authentication");
            return false;
        }
        finally
        {
            this.authLock.Release();
        }
    }

    private async Task<HttpResponseMessage> MakeAuthenticatedRequestAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        byte[]? contentBytes = null;
        string? contentMediaType = null;

        if (content != null)
        {
            contentBytes = await content.ReadAsByteArrayAsync();
            if (content.Headers.ContentType != null)
            {
                contentMediaType = content.Headers.ContentType.ToString();
            }
        }

        if (string.IsNullOrEmpty(this.authCookie))
        {
            await this.AuthenticateAsync();
        }

        HttpRequestMessage initialRequest = CreateRequest();
        HttpResponseMessage response = await this.httpClient.SendAsync(initialRequest);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        this.authCookie = null;
        await this.AuthenticateAsync();
        HttpRequestMessage retryRequest = CreateRequest();
        response = await this.httpClient.SendAsync(retryRequest);
        return response;

        HttpRequestMessage CreateRequest()
        {
            HttpRequestMessage request = new(method, url);

            if (contentBytes != null)
            {
                ByteArrayContent newContent = new(contentBytes);
                if (!string.IsNullOrEmpty(contentMediaType))
                {
                    newContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentMediaType);
                }
                request.Content = newContent;
            }

            if (!string.IsNullOrEmpty(this.authCookie))
            {
                request.Headers.Add("Cookie", this.authCookie);
            }

            return request;
        }
    }

    public async Task<PortStatus?> GetPortStatusAsync(int portNumber)
    {
        try
        {
            string url = $"/api/s/{this.config.SiteName}/stat/device/{this.config.SwitchMac}";
            HttpResponseMessage response = await this.MakeAuthenticatedRequestAsync(HttpMethod.Get, url);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError("Failed to get port status: {StatusCode}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonElement data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("data", out JsonElement dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement device in dataArray.EnumerateArray())
                {
                    if (device.TryGetProperty("port_table", out JsonElement portTable))
                    {
                        foreach (JsonElement port in portTable.EnumerateArray())
                        {
                            if (port.TryGetProperty("port_idx", out JsonElement portIdx) && portIdx.GetInt32() == portNumber)
                            {
                                PortConfiguration? portConfig = await this.configService.GetPortConfigurationAsync(portNumber);
                                return new PortStatus
                                {
                                    PortNumber = portNumber,
                                    PortName = portConfig?.PortName ?? $"Port {portNumber}",
                                    IsEnabled = port.TryGetProperty("up", out JsonElement up) && up.GetBoolean(),
                                    PoeEnabled = port.TryGetProperty("poe_enable", out JsonElement poeEnable) && poeEnable.GetBoolean()
                                };
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting port status for port {PortNumber}", portNumber);
            return null;
        }
    }

    public async Task<List<PortStatus>> GetAllPortsStatusAsync()
    {
        List<PortStatus> statuses = [];
        List<PortConfiguration> portConfigs = await this.configService.GetAllPortConfigurationsAsync();

        foreach (PortConfiguration config in portConfigs)
        {
            PortStatus? status = await this.GetPortStatusAsync(config.PortNumber);
            if (status != null)
            {
                statuses.Add(status);
            }
        }

        return statuses;
    }

    public async Task<bool> SetPortStateAsync(int portNumber, bool enable)
    {
        try
        {
            string url = $"/api/s/{this.config.SiteName}/rest/device/{this.config.SwitchMac}";

            var payload = new
            {
                port_overrides = new[]
                {
                    new
                    {
                        port_idx = portNumber,
                        poe_mode = enable ? "auto" : "off"
                    }
                }
            };

            StringContent content = new(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await this.MakeAuthenticatedRequestAsync(HttpMethod.Put, url, content);

            if (response.IsSuccessStatusCode)
            {
                this.logger.LogInformation("Successfully set port {PortNumber} to {State}", portNumber, enable ? "enabled" : "disabled");
                return true;
            }

            this.logger.LogError("Failed to set port state: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting port state for port {PortNumber}", portNumber);
            return false;
        }
    }
}
