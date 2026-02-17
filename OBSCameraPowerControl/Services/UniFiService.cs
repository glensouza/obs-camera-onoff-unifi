using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OBSCameraPowerControl.Models;

namespace OBSCameraPowerControl.Services;

public class UniFiService
{
    private readonly HttpClient httpClient;
    private readonly UniFiConfiguration config;
    private readonly ILogger<UniFiService> logger;
    private readonly SemaphoreSlim authLock = new(1, 1);
    private string? authCookie;
    private string? csrfToken;
    // Auto-detected API prefix: empty for classic controller, "/proxy/network" for UniFi OS
    private string apiPrefix = string.Empty;
    private bool apiStyleDetected;
    // Cached device _id from the stat response (required for REST PUT on UniFi OS)
    private string? deviceId;
    // In-memory set of port numbers the user wants displayed
    private readonly HashSet<int> monitoredPorts = [];

    public UniFiService(HttpClient httpClient, IOptions<UniFiConfiguration> config, ILogger<UniFiService> logger)
    {
        this.httpClient = httpClient;
        this.config = config.Value;
        this.logger = logger;
        this.httpClient.BaseAddress = new Uri(this.config.Host.TrimEnd('/'));
    }

    /// <summary>
    /// Returns the set of port numbers currently being monitored.
    /// </summary>
    public IReadOnlySet<int> MonitoredPorts => this.monitoredPorts;

    /// <summary>
    /// Adds a port to the monitored set.
    /// </summary>
    public void AddMonitoredPort(int portNumber)
    {
        this.monitoredPorts.Add(portNumber);
    }

    /// <summary>
    /// Removes a port from the monitored set.
    /// </summary>
    public void RemoveMonitoredPort(int portNumber)
    {
        this.monitoredPorts.Remove(portNumber);
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

            var loginData = new
            {
                username = this.config.Username,
                password = this.config.Password
            };

            string jsonPayload = JsonSerializer.Serialize(loginData);

            // Try UniFi OS login first, then fall back to classic controller
            string[] loginUrls = ["/api/auth/login", "/api/login"];
            string[] prefixes = ["/proxy/network", ""];

            for (int i = 0; i < loginUrls.Length; i++)
            {
                StringContent content = new(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await this.httpClient.PostAsync(loginUrls[i], content);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    this.logger.LogInformation("Login endpoint {LoginUrl} returned 404, trying next", loginUrls[i]);
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
                    {
                        this.authCookie = string.Join("; ", cookies);
                        this.apiPrefix = prefixes[i];
                        this.apiStyleDetected = true;

                        // UniFi OS returns a CSRF token required for state-changing requests
                        if (response.Headers.TryGetValues("X-CSRF-Token", out IEnumerable<string>? csrfValues))
                        {
                            this.csrfToken = csrfValues.FirstOrDefault();
                        }

                        this.logger.LogInformation(
                            "Successfully authenticated to UniFi controller via {LoginUrl} (API prefix: '{ApiPrefix}')",
                            loginUrls[i],
                            this.apiPrefix);
                        return true;
                    }
                }

                this.logger.LogError("Authentication failed at {LoginUrl}: {StatusCode}", loginUrls[i], response.StatusCode);
                return false;
            }

            this.logger.LogError("Authentication failed: no working login endpoint found. Verify the Host URL and port (UDM devices use port 443, classic controllers use 8443)");
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
        this.csrfToken = null;
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

            if (!string.IsNullOrEmpty(this.csrfToken))
            {
                request.Headers.Add("X-CSRF-Token", this.csrfToken);
            }

            return request;
        }
    }

    /// <summary>
    /// Gets the status of all PoE-capable ports directly from the UniFi switch.
    /// </summary>
    public async Task<List<PortStatus>> GetAllPortsStatusAsync()
    {
        List<PortStatus> statuses = [];

        try
        {
            // Query all devices — more compatible across controller versions than /stat/device/{mac}
            string url = $"{this.apiPrefix}/api/s/{this.config.SiteName}/stat/device";
            HttpResponseMessage response = await this.MakeAuthenticatedRequestAsync(HttpMethod.Get, url);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError("Failed to get devices: {StatusCode}", response.StatusCode);
                return statuses;
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonElement data = JsonSerializer.Deserialize<JsonElement>(json);

            if (!data.TryGetProperty("data", out JsonElement dataArray) || dataArray.ValueKind != JsonValueKind.Array)
            {
                this.logger.LogWarning("Unexpected response format from UniFi controller");
                return statuses;
            }

            // Find the target switch by MAC address
            string targetMac = this.config.SwitchMac.ToLowerInvariant();
            foreach (JsonElement device in dataArray.EnumerateArray())
            {
                string deviceMac = device.TryGetProperty("mac", out JsonElement macEl)
                    ? macEl.GetString()?.ToLowerInvariant() ?? string.Empty
                    : string.Empty;

                if (deviceMac != targetMac)
                {
                    continue;
                }

                // Cache device _id for REST API calls (UniFi OS uses _id, not MAC)
                if (device.TryGetProperty("_id", out JsonElement idEl))
                {
                    this.deviceId = idEl.GetString();
                }

                if (!device.TryGetProperty("port_table", out JsonElement portTable))
                {
                    this.logger.LogWarning("Device {Mac} found but has no port_table", targetMac);
                    return statuses;
                }

                foreach (JsonElement port in portTable.EnumerateArray())
                {
                    // Only include ports that have PoE capability
                    bool hasPoeCapability = port.TryGetProperty("poe_caps", out JsonElement poeCaps) && poeCaps.GetInt32() > 0;
                    bool hasPoeEnable = port.TryGetProperty("poe_enable", out JsonElement poeEnable);

                    if (!hasPoeCapability && !hasPoeEnable)
                    {
                        continue;
                    }

                    int portNumber = port.TryGetProperty("port_idx", out JsonElement portIdx) ? portIdx.GetInt32() : 0;
                    string portName = port.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() ?? $"Port {portNumber}" : $"Port {portNumber}";

                    statuses.Add(new PortStatus
                    {
                        PortNumber = portNumber,
                        PortName = portName,
                        IsEnabled = port.TryGetProperty("up", out JsonElement up) && up.GetBoolean(),
                        PoeEnabled = hasPoeEnable && poeEnable.GetBoolean()
                    });
                }

                return statuses;
            }

            this.logger.LogWarning("Switch with MAC {SwitchMac} not found. Available devices: {DeviceCount}", targetMac, dataArray.GetArrayLength());
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting port statuses from UniFi switch");
        }

        return statuses;
    }

    public async Task<bool> SetPortStateAsync(int portNumber, bool enable)
    {
        try
        {
            // UniFi OS REST endpoint requires device _id; classic controller uses MAC
            string deviceIdentifier = this.deviceId ?? this.config.SwitchMac;
            string url = $"{this.apiPrefix}/api/s/{this.config.SiteName}/rest/device/{deviceIdentifier}";

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
