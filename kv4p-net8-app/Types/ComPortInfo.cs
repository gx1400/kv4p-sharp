namespace kv4p_net8_app.Types;

public class ComPortInfo(string portNumber, string deviceName)
{
    public string PortNumber { get; set; } = portNumber;
    public string DeviceName { get; set; } = deviceName;

    public override string ToString() => $" ({PortNumber}) {DeviceName}";

    public string ComboBoxText => $"{DeviceName}";
}
