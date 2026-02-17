using OBSCameraPowerControl.Models;

namespace OBSCameraPowerControl.Services;

public interface IConfigurationService
{
    Task<PortConfiguration?> GetPortConfigurationAsync(int portNumber);
    Task<List<PortConfiguration>> GetAllPortConfigurationsAsync();
    Task SavePortConfigurationAsync(PortConfiguration config);
}
