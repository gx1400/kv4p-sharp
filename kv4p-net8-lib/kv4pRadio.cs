using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using kv4p_net8_lib.Enum;
using kv4p_net8_lib.Interface;

namespace kv4p_net8_lib
{
    public partial class kv4pRadio : ObservableObject, IDisposable
    {
        private ISerialPort _serialPort;
        public EventHandler<byte[]> DataBytesReceivedEventHandler;

        private static readonly byte[] COMMAND_DELIMITER = new byte[]
        {
            0xFF,
            0x00,
            0xFF,
            0x00,
            0xFF,
            0x00,
            0xFF,
            0x00,
        };

        private const int MIN_FIRMWARE_VER = 1;

        // Synchronization locks
        private readonly object _syncLock = new object();
        private readonly object _versionStrBufferLock = new object();

        /// <summary>
        /// Occurs when an error is encountered.
        /// </summary>
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Occurs when audio data is received in RX mode.
        /// </summary>
        public event EventHandler<byte[]> AudioDataReceived;

        [ObservableProperty]
        private RadioModeEnum currentMode;

        [ObservableProperty]
        private string versionStrBuffer;

        public kv4pRadio(ISerialPort serialPort)
        {
            _serialPort = serialPort;
            _serialPort.DataReceived += _serialPort_DataReceived;
        }

        private void _serialPort_DataReceived(object? sender, EventArgs e)
        {
            byte[] receivedData = null;
            lock (_syncLock)
            {
                try
                {
                    int bytesToRead = _serialPort.BytesToRead;
                    receivedData = new byte[bytesToRead];
                    _serialPort.Read(receivedData, 0, bytesToRead);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ErrorEventArgs(ex));
                }
            }
            if (receivedData != null && receivedData.Length > 0)
            {
                HandleData(receivedData);
            }
        }

        private string MakeSafe2MFreq(string strFreq)
        {
            // Implement frequency validation and formatting as needed
            if (
                !float.TryParse(
                    strFreq,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float freq
                )
            )
            {
                freq = 146.520f; // Default frequency
            }

            while (freq > 148.0f)
            {
                freq /= 10f;
            }

            freq = Math.Min(freq, 148.0f);
            freq = Math.Max(freq, 144.0f);

            string formattedFreq = freq.ToString(
                "000.000",
                System.Globalization.CultureInfo.InvariantCulture
            );
            return formattedFreq;
        }

        public void TuneToFrequency(
            string txFrequencyStr,
            string rxFrequencyStr,
            int tone,
            int squelchLevel
        )
        {
            if (string.IsNullOrWhiteSpace(txFrequencyStr))
                throw new ArgumentException(
                    "Transmit frequency cannot be null or empty.",
                    nameof(txFrequencyStr)
                );

            if (string.IsNullOrWhiteSpace(rxFrequencyStr))
                throw new ArgumentException(
                    "Receive frequency cannot be null or empty.",
                    nameof(rxFrequencyStr)
                );

            txFrequencyStr = MakeSafe2MFreq(txFrequencyStr);
            rxFrequencyStr = MakeSafe2MFreq(rxFrequencyStr);

            string toneStr = tone.ToString("00");
            string squelchStr = squelchLevel.ToString();

            // Ensure squelch level is a single digit
            if (squelchStr.Length != 1)
                throw new ArgumentException(
                    "Squelch level must be a single digit (0-9).",
                    nameof(squelchLevel)
                );

            // Build parameters string
            string paramsStr = txFrequencyStr + rxFrequencyStr + toneStr + squelchStr;
            SendCommand(Esp32CommandEnum.TUNE_TO, paramsStr);
        }

        private void HandleData(byte[] data)
        {
            DataBytesReceivedEventHandler?.Invoke(this, data);
            RadioModeEnum mode;
            lock (_syncLock)
            {
                mode = CurrentMode;
            }

            switch (mode)
            {
                case RadioModeEnum.STARTUP:
                    HandleStartupData(data);
                    break;
                case RadioModeEnum.RX:
                case RadioModeEnum.SCAN:
                    OnAudioDataReceived(data);
                    break;
                case RadioModeEnum.TX:
                    break;
                default:
                    break;
            }
        }

        private void HandleStartupData(byte[] data)
        {
            // Handle firmware version check
            string dataStr = System.Text.Encoding.UTF8.GetString(data);
            lock (_versionStrBufferLock)
            {
                versionStrBuffer += dataStr;
                if (versionStrBuffer.Contains("VERSION"))
                {
                    int startIdx = versionStrBuffer.IndexOf("VERSION") + "VERSION".Length;
                    if (versionStrBuffer.Length >= startIdx + 8)
                    {
                        string verStr = versionStrBuffer.Substring(startIdx, 8);
                        if (int.TryParse(verStr, out int verInt))
                        {
                            if (verInt < MIN_FIRMWARE_VER)
                            {
                                OnErrorOccurred(
                                    new ErrorEventArgs(
                                        new InvalidOperationException(
                                            "Unsupported firmware version."
                                        )
                                    )
                                );
                            }
                            else
                            {
                                lock (_syncLock)
                                {
                                    currentMode = RadioModeEnum.RX;
                                }
                                // No need to initialize audio playback
                            }
                        }
                        else
                        {
                            OnErrorOccurred(
                                new ErrorEventArgs(
                                    new FormatException("Invalid firmware version format.")
                                )
                            );
                        }
                        versionStrBuffer = string.Empty;
                    }
                }
            }
        }

        public void Initialize()
        {
            lock (_syncLock)
            {
                CurrentMode = RadioModeEnum.STARTUP;
            }
            SendCommand(Esp32CommandEnum.STOP);
            SendCommand(Esp32CommandEnum.GET_FIRMWARE_VER);
        }

        private void SendCommand(Esp32CommandEnum command)
        {
            byte[] commandArray = new byte[COMMAND_DELIMITER.Length + 1];
            Array.Copy(COMMAND_DELIMITER, commandArray, COMMAND_DELIMITER.Length);
            commandArray[COMMAND_DELIMITER.Length] = (byte)command;
            SendBytesToEsp32(commandArray);
        }

        private void SendCommand(Esp32CommandEnum command, string paramsStr)
        {
            byte[] paramsBytes = System.Text.Encoding.ASCII.GetBytes(paramsStr);
            byte[] commandArray = new byte[COMMAND_DELIMITER.Length + 1 + paramsBytes.Length];
            Array.Copy(COMMAND_DELIMITER, commandArray, COMMAND_DELIMITER.Length);
            commandArray[COMMAND_DELIMITER.Length] = (byte)command;
            Array.Copy(
                paramsBytes,
                0,
                commandArray,
                COMMAND_DELIMITER.Length + 1,
                paramsBytes.Length
            );
            SendBytesToEsp32(commandArray);
        }

        private void SendBytesToEsp32(byte[] data)
        {
            lock (_syncLock)
            {
                if (_serialPort.IsOpen)
                {
                    try
                    {
                        _serialPort.Write(data, 0, data.Length);
                    }
                    catch (TimeoutException ex)
                    {
                        OnErrorOccurred(new ErrorEventArgs(ex));
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred(new ErrorEventArgs(ex));
                    }
                }
            }
        }

        public void StartRXMode()
        {
            lock (_syncLock)
            {
                currentMode = RadioModeEnum.RX;
            }
        }

        public void StartTXMode()
        {
            lock (_syncLock)
            {
                currentMode = RadioModeEnum.TX;
                SendCommand(Esp32CommandEnum.PTT_DOWN);
            }
        }

        public void EndTXMode()
        {
            lock (_syncLock)
            {
                if (currentMode == RadioModeEnum.TX)
                {
                    SendCommand(Esp32CommandEnum.PTT_UP);
                    currentMode = RadioModeEnum.RX;
                }
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                currentMode = RadioModeEnum.RX;
            }
            SendCommand(Esp32CommandEnum.STOP);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnErrorOccurred(ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnAudioDataReceived(byte[] data)
        {
            AudioDataReceived?.Invoke(this, data);
        }
    }
}
