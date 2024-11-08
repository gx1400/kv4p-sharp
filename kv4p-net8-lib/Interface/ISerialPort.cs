namespace kv4p_net8_lib.Interface;

public interface ISerialPort : IDisposable
{
    string PortName { get; set; }
    bool IsOpen { get; }

    void Open();
    void Close();
    int Read(byte[] buffer, int offset, int count);
    int BytesToRead { get; }
    void Write(byte[] buffer, int offset, int count);
    event EventHandler DataReceived;
}
