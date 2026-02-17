using UniFiCameraControl.Models;

namespace UniFiCameraControl.Services;

public interface IUniFiService
{
    Task<PortStatus?> GetPortStatusAsync(int portNumber);
    Task<List<PortStatus>> GetAllPortsStatusAsync();
    Task<bool> SetPortStateAsync(int portNumber, bool enable);
}
