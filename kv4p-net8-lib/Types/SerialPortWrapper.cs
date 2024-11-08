using System.IO.Ports;
using kv4p_net8_lib.Interface;

namespace kv4p_net8_lib.Types;

public class SerialPortWrapper : ISerialPort
{
    private readonly SerialPort _serialPort;
    private const int BaudRate = 921600;
    private const int DataBits = 8;
    private const Parity Parity = System.IO.Ports.Parity.None;
    private const StopBits StopBits = System.IO.Ports.StopBits.One;
    private const int ReadTimeout = 1000;
    private const int WriteTimeout = 1000;

    public SerialPortWrapper(string portName)
    {
        _serialPort = new SerialPort(portName)
        {
            BaudRate = BaudRate,
            DataBits = DataBits,
            Parity = Parity,
            StopBits = StopBits,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
        };
        _serialPort.DataReceived += (s, e) => DataReceived?.Invoke(this, e);
    }

    public SerialPortWrapper()
    {
        _serialPort = new SerialPort
        {
            BaudRate = BaudRate,
            DataBits = DataBits,
            Parity = Parity,
            StopBits = StopBits,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
        };
    }

    public string PortName
    {
        get => _serialPort.PortName;
        set => _serialPort.PortName = value;
    }

    public bool IsOpen => _serialPort.IsOpen;

    public void Open() => _serialPort.Open();

    public void Close() => _serialPort.Close();

    public int BytesToRead => _serialPort.BytesToRead;

    public int Read(byte[] buffer, int offset, int count) =>
        _serialPort.Read(buffer, offset, count);

    public void Write(byte[] buffer, int offset, int count) =>
        _serialPort.Write(buffer, offset, count);

    public event EventHandler? DataReceived;

    public void Dispose() => _serialPort.Dispose();
}
