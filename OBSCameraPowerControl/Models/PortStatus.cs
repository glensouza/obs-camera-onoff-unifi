namespace OBSCameraPowerControl.Models;

public class PortStatus
{
    public int PortNumber { get; set; }
    public string PortName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool PoeEnabled { get; set; }
}
