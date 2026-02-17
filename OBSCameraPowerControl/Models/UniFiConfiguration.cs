namespace OBSCameraPowerControl.Models;

public class UniFiConfiguration
{
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SiteName { get; set; } = "default";
    public string SwitchMac { get; set; } = string.Empty;
    public bool IgnoreSslErrors { get; set; } = true;
}
