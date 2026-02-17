using Azure;
using Azure.Data.Tables;
using OBSCameraPowerControl.Models;

namespace OBSCameraPowerControl.Services;

public class ConfigurationService(TableServiceClient tableServiceClient, ILogger<ConfigurationService> logger) : IConfigurationService
{
    private readonly TableClient tableClient = tableServiceClient.GetTableClient(TableName);
    private const string TableName = "PortConfigurations";
    private bool initialized;
    private readonly SemaphoreSlim initLock = new(1, 1);

    private async Task EnsureInitializedAsync()
    {
        if (this.initialized) return;

        await this.initLock.WaitAsync();
        try
        {
            if (this.initialized) return;

            await this.tableClient.CreateIfNotExistsAsync();

            PortConfiguration[] defaultConfigs =
            [
                new() { PortNumber = 1, PortName = "Camera 1" },
                new() { PortNumber = 2, PortName = "Camera 2" },
                new() { PortNumber = 3, PortName = "Camera 3" }
            ];

            foreach (PortConfiguration config in defaultConfigs)
            {
                PortConfigurationEntity entity = new()
                {
                    PartitionKey = "Default",
                    RowKey = config.PortNumber.ToString(),
                    PortNumber = config.PortNumber,
                    PortName = config.PortName
                };

                try
                {
                    await this.tableClient.GetEntityAsync<PortConfigurationEntity>("Default", config.PortNumber.ToString());
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    await this.tableClient.AddEntityAsync(entity);
                    logger.LogInformation("Created default configuration for port {PortNumber}", config.PortNumber);
                }
            }

            this.initialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing default configurations");
        }
        finally
        {
            this.initLock.Release();
        }
    }

    public async Task<PortConfiguration?> GetPortConfigurationAsync(int portNumber)
    {
        await this.EnsureInitializedAsync();

        try
        {
            Response<PortConfigurationEntity>? entity = await this.tableClient.GetEntityAsync<PortConfigurationEntity>("Default", portNumber.ToString());
            return new PortConfiguration
            {
                PortNumber = entity.Value.PortNumber,
                PortName = entity.Value.PortName
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Port configuration not found for port {PortNumber}", portNumber);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving port configuration for port {PortNumber}", portNumber);
            return null;
        }
    }

    public async Task<List<PortConfiguration>> GetAllPortConfigurationsAsync()
    {
        await this.EnsureInitializedAsync();

        List<PortConfiguration> configurations = [];

        try
        {
            await foreach (PortConfigurationEntity? entity in this.tableClient.QueryAsync<PortConfigurationEntity>(filter: "PartitionKey eq 'Default'"))
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
            logger.LogError(ex, "Error retrieving all port configurations");
        }

        return configurations;
    }

    public async Task SavePortConfigurationAsync(PortConfiguration config)
    {
        await this.EnsureInitializedAsync();

        try
        {
            PortConfigurationEntity entity = new()
            {
                PartitionKey = "Default",
                RowKey = config.PortNumber.ToString(),
                PortNumber = config.PortNumber,
                PortName = config.PortName
            };

            await this.tableClient.UpsertEntityAsync(entity);
            logger.LogInformation("Saved configuration for port {PortNumber}", config.PortNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving port configuration for port {PortNumber}", config.PortNumber);
            throw;
        }
    }

    private class PortConfigurationEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public int PortNumber { get; init; }
        public string PortName { get; init; } = string.Empty;
    }
}
