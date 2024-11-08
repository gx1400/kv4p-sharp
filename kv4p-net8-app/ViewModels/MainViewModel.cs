using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Mime;
using System.Text;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kv4p_net8_app.Models;
using kv4p_net8_app.Types;
using kv4p_net8_app.ValidationRule;
using kv4p_net8_lib.Enum;
using kv4p_net8_lib.Types;
using Microsoft.Extensions.Logging;

namespace kv4p_net8_app.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RadioModel _radioModel;

    private readonly StringBuilder _outputText = new();

    [ObservableProperty]
    private string displayText;

    public ObservableCollection<ComPortInfo?> ComPorts => _radioModel.ComPortList;
    public ObservableCollection<OffsetDirectionEnum> OffsetDirections { get; }
    public Dictionary<int, string> CtcssTones { get; private set; }

    public Collection<System.Windows.Controls.ValidationRule> ValidationRules { get; }

    [ObservableProperty]
    public ComPortInfo? selectedComPort;

    [ObservableProperty]
    public bool isComboBoxEnabled;

    [ObservableProperty]
    private bool serialPortConnected; // Backing field for SerialPortConnected

    [ObservableProperty]
    private bool filterEmphasis;

    [ObservableProperty]
    private bool filterHighPass;

    [ObservableProperty]
    private bool filterLowPass;

    [ObservableProperty]
    private float frequencyMhz;

    [ObservableProperty]
    private int squelchLevel;

    public MainViewModel(ILogger<MainViewModel> logger, RadioModel radioModel)
    {
        OffsetDirections = new ObservableCollection<OffsetDirectionEnum>(
            (OffsetDirectionEnum[])Enum.GetValues(typeof(OffsetDirectionEnum))
        );

        _radioModel = radioModel;
        _radioModel.ComPortList.CollectionChanged += OnComPortListChanged;
        _radioModel.OnSerialPortConnectedChanged += UpdateSerialPortConnected; // Subscribe to event

        _radioModel.DataReceived += _radioModel_DataReceived;

        CtcssTones = (new CtcssTones()).TonesDictionary;
        frequencyMhz = 146.520f;

        ValidationRules = new();
        ValidationRules.Add(new DecimalFormatValidationRule());

        UpdateComboBoxState();
    }

    private void _radioModel_DataReceived(object? sender, byte[] e)
    {
        if (e.Length > 20) //TODO shortcut to prevent overloading the text box
            return;

        // Convert byte array to string
        string text = System.Text.Encoding.ASCII.GetString(e);

        // Append text to the StringBuilder
        _outputText.Append(text);

        // Update the DisplayText property with the new data
        DisplayText = _outputText.ToString();
    }

    private void UpdateSerialPortConnected(object? sender, bool e)
    {
        SerialPortConnected = _radioModel.IsSerialPortConnected;
    }

    private void OnComPortListChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateComboBoxState();
    }

    private void UpdateComboBoxState()
    {
        if (ComPorts.Count == 0)
        {
            SelectedComPort = new ComPortInfo("", "No device connected.");
            IsComboBoxEnabled = false;
        }
        else
        {
            IsComboBoxEnabled = true;
            if (SelectedComPort == null || !ComPorts.Contains(SelectedComPort))
            {
                SelectedComPort = ComPorts[0]; // Select the first item by default
            }
        }
    }

    [RelayCommand]
    public void Connect()
    {
        _outputText.Clear();
        DisplayText = _outputText.ToString();
        ComPortInfo? portInfo = SelectedComPort;
        _radioModel.Connect(portInfo);
    }

    [RelayCommand]
    public void Disconnect() => _radioModel.Disconnect();

    public void Dispose()
    {
        _radioModel.Dispose();
    }
}
