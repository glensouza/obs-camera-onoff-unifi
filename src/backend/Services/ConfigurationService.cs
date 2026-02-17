using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using UniFiCameraControl.Models;

namespace UniFiCameraControl.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<ConfigurationService> _logger;
    private const string TableName = "PortConfigurations";

    public ConfigurationService(
        TableServiceClient tableServiceClient,
        ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _tableClient.CreateIfNotExists();
        
        // Initialize default configurations for 3 ports if they don't exist
        InitializeDefaultConfigurationsAsync().Wait();
    }

    private async Task InitializeDefaultConfigurationsAsync()
    {
        try
        {
            var defaultConfigs = new[]
            {
                new PortConfiguration { PortNumber = 1, PortName = "Camera 1" },
                new PortConfiguration { PortNumber = 2, PortName = "Camera 2" },
                new PortConfiguration { PortNumber = 3, PortName = "Camera 3" }
            };

            foreach (var config in defaultConfigs)
            {
                var entity = new PortConfigurationEntity
                {
                    PartitionKey = "Default",
                    RowKey = config.PortNumber.ToString(),
                    PortNumber = config.PortNumber,
                    PortName = config.PortName
                };

                try
                {
                    await _tableClient.GetEntityAsync<PortConfigurationEntity>("Default", config.PortNumber.ToString());
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    await _tableClient.AddEntityAsync(entity);
                    _logger.LogInformation("Created default configuration for port {PortNumber}", config.PortNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default configurations");
        }
    }

    public async Task<PortConfiguration?> GetPortConfigurationAsync(int portNumber)
    {
        try
        {
            var entity = await _tableClient.GetEntityAsync<PortConfigurationEntity>("Default", portNumber.ToString());
            return new PortConfiguration
            {
                PortNumber = entity.Value.PortNumber,
                PortName = entity.Value.PortName
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Port configuration not found for port {PortNumber}", portNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving port configuration for port {PortNumber}", portNumber);
            return null;
        }
    }

    public async Task<List<PortConfiguration>> GetAllPortConfigurationsAsync()
    {
        var configurations = new List<PortConfiguration>();

        try
        {
            await foreach (var entity in _tableClient.QueryAsync<PortConfigurationEntity>(filter: "PartitionKey eq 'Default'"))
            {
                configurations.Add(new PortConfiguration
                {
                    PortNumber = entity.PortNumber,
                    PortName = entity.PortName
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all port configurations");
        }

        return configurations;
    }

    public async Task SavePortConfigurationAsync(PortConfiguration config)
    {
        try
        {
            var entity = new PortConfigurationEntity
            {
                PartitionKey = "Default",
                RowKey = config.PortNumber.ToString(),
                PortNumber = config.PortNumber,
                PortName = config.PortName
            };

            await _tableClient.UpsertEntityAsync(entity);
            _logger.LogInformation("Saved configuration for port {PortNumber}", config.PortNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving port configuration for port {PortNumber}", config.PortNumber);
            throw;
        }
    }

    private class PortConfigurationEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int PortNumber { get; set; }
        public string PortName { get; set; } = string.Empty;
    }
}
