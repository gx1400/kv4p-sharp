using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using kv4p_net8_app.Types;
using kv4p_net8_lib;
using kv4p_net8_lib.Interface;
using kv4p_net8_lib.Types;

namespace kv4p_net8_app.Models;

public partial class RadioModel : ObservableObject, IDisposable
{
    private ManagementEventWatcher? _usbWatcher;

    private ISerialPort _serialPort;
    private kv4pRadio _radio;

    public event EventHandler<byte[]> DataReceived;

    private bool _isSerialPortConnected;
    public bool IsSerialPortConnected
    {
        get => _isSerialPortConnected;
        private set
        {
            if (_isSerialPortConnected != value)
            {
                _isSerialPortConnected = value;
                OnSerialPortConnectedChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<bool>? OnSerialPortConnectedChanged;

    [ObservableProperty]
    // ReSharper disable once InconsistentNaming
    private ObservableCollection<ComPortInfo> comPortList = new();

    [ObservableProperty]
    // ReSharper disable once InconsistentNaming
    private bool filterByVidPid = true;

    [ObservableProperty]
    // ReSharper disable once InconsistentNaming
    private ComPortInfo? selectedComPort;

    [ObservableProperty]
    private bool filterEmphasis;

    [ObservableProperty]
    private bool filterHighPass;

    [ObservableProperty]
    private bool filterLowPass;

    private List<int> _acceptableVids = new() { 4292 };
    private List<int> _acceptablePids = new() { 60000 };

    public RadioModel(ISerialPort serialPort)
    {
        BindingOperations.EnableCollectionSynchronization(ComPortList, new object());
        _serialPort = serialPort;

        StartUsbMonitoring();
        UpdateComPortList();
    }

    public void Connect()
    {
        if (_serialPort is null)
            throw new NullReferenceException("serial port is null");
        _serialPort.Open();
    }

    public void Connect(ComPortInfo? portInfo)
    {
        if (portInfo is null)
            return;

        SelectedComPort = portInfo;

        _serialPort = new SerialPortWrapper(portInfo.PortNumber);
        if (!_serialPort.IsOpen)
        {
            ResetSettings();

            _serialPort.Open();
            IsSerialPortConnected = true;
            StopUsbMonitoring();

            _radio = new kv4pRadio(_serialPort);
            _radio.DataBytesReceivedEventHandler += (sender, data) =>
                DataReceived?.Invoke(this, data);
            _radio.Initialize();
            _radio.TuneToFrequency("144.390", "144.390", 0, 0);
        }
    }

    public void TuneToFrequency(string frequency)
    {
        if (_radio is not null)
        {
            _radio.TuneToFrequency(frequency, frequency, 0, 0);
        }
    }

    private void ResetSettings()
    {
        FilterEmphasis = false;
        FilterHighPass = false;
        FilterLowPass = false;
    }

    public void Disconnect()
    {
        if (_serialPort is null)
            return;

        IsSerialPortConnected = false;
        _serialPort.Close();

        ResetSettings();

        StartUsbMonitoring();
    }

    private void StartUsbMonitoring()
    {
        if (_usbWatcher != null || _isSerialPortConnected)
            return;

        var query = new WqlEventQuery(
            "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3"
        );
        _usbWatcher = new ManagementEventWatcher(query);
        _usbWatcher.EventArrived += OnDeviceChanged;
        _usbWatcher.Start();
    }

    private void StopUsbMonitoring()
    {
        _usbWatcher?.Stop();
        _usbWatcher?.Dispose();
        _usbWatcher = null;
    }

    private void OnDeviceChanged(object sender, EventArrivedEventArgs e)
    {
        UpdateComPortList();
    }

    private void UpdateComPortList()
    {
        if (_isSerialPortConnected)
            return;

        if (comPortList is null)
            return;

        comPortList.Clear();
        foreach (string portName in SerialPort.GetPortNames())
        {
            if (filterByVidPid && !IsAcceptedDevice(portName))
                continue;

            string deviceName = GetDeviceName(portName);

            comPortList.Add(new ComPortInfo(portName, deviceName));
        }
    }

    private string GetDeviceName(string portName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
            foreach (var device in searcher.Get())
            {
                string? name = device["Name"]?.ToString();
                string? deviceId = device["DeviceID"]?.ToString();

                // Match the COM port name
                if (name?.Contains(portName) == true && deviceId != null)
                {
                    return name;
                }
            }
        }
        catch
        {
            // Log or handle errors in retrieving the device name
        }

        // Fallback if device name is not found
        return "Unknown Device";
    }

    private int GetPropertyFromDeviceId(string deviceId, string property)
    {
        string result = "";
        string pattern = property + "_([0-9A-F]{4})";
        var match = Regex.Match(deviceId, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            result = match.Groups[1].Value;
        }
        return Convert.ToInt32(result, 16);
    }

    private bool IsAcceptedDevice(string portName)
    {
        try
        {
            using (
                var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"
                )
            )
            {
                var devices = searcher.Get();
                foreach (var device in devices)
                {
                    string? name = device["Name"]?.ToString();
                    if (name?.Contains(portName) != true)
                        continue;
                    string? pnpDeviceId = device["PNPDeviceID"].ToString();

                    int vid;
                    int pid;
                    if (
                        pnpDeviceId is not null
                        && pnpDeviceId.Contains("VID")
                        && pnpDeviceId.Contains("PID")
                    )
                    {
                        vid = GetPropertyFromDeviceId(pnpDeviceId, "VID");
                        pid = GetPropertyFromDeviceId(pnpDeviceId, "PID");
                    }
                    else if (
                        pnpDeviceId is not null
                        && pnpDeviceId.Contains("VEN")
                        && pnpDeviceId.Contains("DEV")
                    )
                    {
                        vid = GetPropertyFromDeviceId(pnpDeviceId, "VEN");
                        pid = GetPropertyFromDeviceId(pnpDeviceId, "DEV");
                    }
                    else
                    {
                        return true;
                    }
                    // Match the filter criteria
                    return (_acceptablePids.Contains(pid) && _acceptableVids.Contains(vid));
                }
            }
        }
        catch
        {
            // Log or handle errors in retrieving the device name
        }

        // Fallback if device name is not found
        return true;
    }

    public void Dispose()
    {
        if (_radio is not null)
        {
            _radio.Dispose();
        }

        if (_serialPort is not null)
        {
            if (_serialPort.IsOpen || _isSerialPortConnected)
                _serialPort.Close();
            _serialPort.Dispose();
        }

        StopUsbMonitoring();
    }
}
