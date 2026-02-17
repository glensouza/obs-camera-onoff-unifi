using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OBSCameraPowerControl.Models;

namespace OBSCameraPowerControl.Services;

public class UniFiService : IUniFiService
{
    private readonly HttpClient _httpClient;
    private readonly UniFiConfiguration _config;
    private readonly ILogger<UniFiService> _logger;
    private readonly IConfigurationService _configService;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private string? _authCookie;

    public UniFiService(
        HttpClient httpClient,
        IOptions<UniFiConfiguration> config,
        ILogger<UniFiService> logger,
        IConfigurationService configService)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _configService = configService;
        _httpClient.BaseAddress = new Uri(_config.Host.TrimEnd('/'));
    }

    private async Task<bool> AuthenticateAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_authCookie))
            {
                return true;
            }

            var loginUrl = "/api/login";
            var loginData = new
            {
                username = _config.Username,
                password = _config.Password
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(loginUrl, content);

            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    _authCookie = string.Join("; ", cookies);
                    _logger.LogInformation("Successfully authenticated to UniFi controller");
                    return true;
                }
            }

            _logger.LogError("Authentication failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return false;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task<HttpResponseMessage> MakeAuthenticatedRequestAsync(
        HttpMethod method,
        string url,
        HttpContent? content = null)
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

        if (string.IsNullOrEmpty(_authCookie))
        {
            await AuthenticateAsync();
        }

        HttpRequestMessage CreateRequest()
        {
            var request = new HttpRequestMessage(method, url);

            if (contentBytes != null)
            {
                var newContent = new ByteArrayContent(contentBytes);
                if (!string.IsNullOrEmpty(contentMediaType))
                {
                    newContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentMediaType);
                }
                request.Content = newContent;
            }

            if (!string.IsNullOrEmpty(_authCookie))
            {
                request.Headers.Add("Cookie", _authCookie);
            }

            return request;
        }

        var initialRequest = CreateRequest();
        var response = await _httpClient.SendAsync(initialRequest);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authCookie = null;
            await AuthenticateAsync();
            var retryRequest = CreateRequest();
            response = await _httpClient.SendAsync(retryRequest);
        }
        return response;
    }

    public async Task<PortStatus?> GetPortStatusAsync(int portNumber)
    {
        try
        {
            var url = $"/api/s/{_config.SiteName}/stat/device/{_config.SwitchMac}";
            var response = await MakeAuthenticatedRequestAsync(HttpMethod.Get, url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get port status: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var device in dataArray.EnumerateArray())
                {
                    if (device.TryGetProperty("port_table", out var portTable))
                    {
                        foreach (var port in portTable.EnumerateArray())
                        {
                            if (port.TryGetProperty("port_idx", out var portIdx) && portIdx.GetInt32() == portNumber)
                            {
                                var portConfig = await _configService.GetPortConfigurationAsync(portNumber);
                                return new PortStatus
                                {
                                    PortNumber = portNumber,
                                    PortName = portConfig?.PortName ?? $"Port {portNumber}",
                                    IsEnabled = port.TryGetProperty("up", out var up) && up.GetBoolean(),
                                    PoeEnabled = port.TryGetProperty("poe_enable", out var poeEnable) && poeEnable.GetBoolean()
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
            _logger.LogError(ex, "Error getting port status for port {PortNumber}", portNumber);
            return null;
        }
    }

    public async Task<List<PortStatus>> GetAllPortsStatusAsync()
    {
        var statuses = new List<PortStatus>();
        var portConfigs = await _configService.GetAllPortConfigurationsAsync();

        foreach (var config in portConfigs)
        {
            var status = await GetPortStatusAsync(config.PortNumber);
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
            var url = $"/api/s/{_config.SiteName}/rest/device/{_config.SwitchMac}";

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

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await MakeAuthenticatedRequestAsync(HttpMethod.Put, url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully set port {PortNumber} to {State}", portNumber, enable ? "enabled" : "disabled");
                return true;
            }

            _logger.LogError("Failed to set port state: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting port state for port {PortNumber}", portNumber);
            return false;
        }
    }
}
