using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SerialMonitorWPF
{
    public partial class MainWindow : Window
    {
        private SerialPort serialPort = new SerialPort();
        private int numberOfLasersPerController = 48;
        public List<int> activeDevices = new List<int>();
        private const byte HEADER_BYTE_A = 0xCA;
        private const byte HEADER_BYTE_B = 0xCB;
        private const byte FOOTER_BYTE = 0x0A;

        private DebugWindow debugWindow = null;

        private bool allLightsOn = false;
        private bool ctrlAOn = false;
        private bool ctrlBOn = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadPorts();
            buttonRefresh.Click += (s, e) => LoadPorts();
            buttonConnect.Click += ButtonConnect_Click;
            serialPort.DataReceived += SerialPort_DataReceived;

            buttonAllOn.Click += ButtonAllOn_Click;
            buttonCtrlAOn.Click += ButtonCtrlAOn_Click;
            buttonCtrlBOn.Click += ButtonCtrlBOn_Click;
            buttonSendRaw.Click += ButtonSendRaw_Click;
            buttonOpenDebug.Click += ButtonOpenDebug_Click;

            for (int i = 0; i < 96; i++)
            {
                activeDevices.Add(i);
            }
        }

        private void LoadPorts()
        {
            comboBoxPorts.ItemsSource = SerialPort.GetPortNames();
            if (comboBoxPorts.Items.Count > 0)
                comboBoxPorts.SelectedIndex = 0;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                try
                {
                    serialPort.PortName = comboBoxPorts.SelectedItem.ToString();
                    serialPort.BaudRate = 115200;
                    serialPort.Open();
                    buttonConnect.Content = "Disconnect";
                    statusText.Text = $"Connected to {serialPort.PortName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect: {ex.Message}");
                }
            }
            else
            {
                serialPort.Close();
                buttonConnect.Content = "Connect";
                statusText.Text = "Disconnected";
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(50);
                int bytesToRead = serialPort.BytesToRead;
                if (bytesToRead < 8)
                    return;
                byte[] receivedPacket = new byte[8];
                int bytesRead = serialPort.Read(receivedPacket, 0, 8);
                if (bytesRead == 8)
                {
                    ProcessDataPacket(receivedPacket);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    textBoxOutputA.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}{Environment.NewLine}");
                    textBoxOutputB.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}{Environment.NewLine}");
                });
            }
        }

        private List<int> GetCutLasers(byte[] message, byte headerByte)
        {
            List<int> cutLasers = new List<int>();
            try
            {
                if (message[0] != headerByte || message[7] != FOOTER_BYTE)
                    throw new ArgumentException($"Invalid message format. Header: 0x{message[0]:X2}, Footer: 0x{message[7]:X2}");
                for (int byteIndex = 1; byteIndex < 7; byteIndex++)
                {
                    byte currentByte = message[byteIndex];
                    for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
                    {
                        int bit = (currentByte >> bitIndex) & 0x1;
                        cutLasers.Add(bit);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCutLasers: {ex.Message}");
                cutLasers.Clear();
            }
            while (cutLasers.Count < 48)
                cutLasers.Add(0);
            return cutLasers;
        }

        private void ProcessDataPacket(byte[] data)
        {
            try
            {
                List<int> cutLasers;
                TextBox targetTextBox;
                if (data[0] == HEADER_BYTE_A)
                {
                    cutLasers = GetCutLasers(data, HEADER_BYTE_A);
                    targetTextBox = textBoxOutputA;
                }
                else if (data[0] == HEADER_BYTE_B)
                {
                    cutLasers = GetCutLasers(data, HEADER_BYTE_B);
                    targetTextBox = textBoxOutputB;
                }
                else
                {
                    throw new ArgumentException($"Unknown header byte: 0x{data[0]:X2}");
                }
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        int rows = 6, cols = 8;
                        if (!int.TryParse(textBoxRows.Text, out rows))
                            rows = 6;
                        if (!int.TryParse(textBoxCols.Text, out cols))
                            cols = 8;
                        StringBuilder displayBuilder = new StringBuilder();
                        displayBuilder.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sensor Grid Data:");
                        displayBuilder.Append("Raw Data (HEX): ");
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (i == 0)
                                displayBuilder.Append($"[{data[i]:X2}] ");
                            else if (i == data.Length - 1)
                                displayBuilder.Append($"[{data[i]:X2}]");
                            else
                                displayBuilder.Append($"{data[i]:X2} ");
                        }
                        displayBuilder.AppendLine();
                        displayBuilder.AppendLine("\nData Bytes to Binary conversion:");
                        for (int i = 1; i < 7; i++)
                        {
                            displayBuilder.Append($"Byte {i} (0x{data[i]:X2}): ");
                            for (int bit = 7; bit >= 0; bit--)
                                displayBuilder.Append((data[i] >> bit) & 0x1);
                            displayBuilder.AppendLine();
                        }
                        displayBuilder.AppendLine("----------------------------------------");
                        displayBuilder.Append("   ");
                        for (int c = 0; c < cols; c++)
                            displayBuilder.Append($"{c,3} ");
                        displayBuilder.AppendLine();
                        for (int r = 0; r < rows; r++)
                        {
                            displayBuilder.Append($"{r,2} ");
                            for (int c = 0; c < cols; c++)
                            {
                                int index = r * cols + c;
                                if (index < cutLasers.Count)
                                    displayBuilder.Append($"{cutLasers[index],3} ");
                                else
                                    displayBuilder.Append("  - ");
                            }
                            displayBuilder.AppendLine();
                        }
                        displayBuilder.AppendLine("----------------------------------------");
                        displayBuilder.AppendLine($"Total Sensors: {cutLasers.Count}");
                        displayBuilder.AppendLine($"Active Sensors: {cutLasers.Count(x => x > 0)}");
                        displayBuilder.AppendLine($"Grid Size: {rows}x{cols}");
                        displayBuilder.AppendLine($"Message Format: Header[0x{data[0]:X2}] + 6 Data Bytes + Footer[0x0A]");
                        displayBuilder.AppendLine();
                        if (targetTextBox.Text.Length > 5000)
                            targetTextBox.Text = targetTextBox.Text.Substring(targetTextBox.Text.Length - 4000);
                        targetTextBox.AppendText(displayBuilder.ToString());
                        targetTextBox.ScrollToEnd();
                    }
                    catch (Exception ex)
                    {
                        targetTextBox.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in UI update: {ex.Message}{Environment.NewLine}");
                        targetTextBox.ScrollToEnd();
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    textBoxOutputA.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in ProcessDataPacket: {ex.Message}{Environment.NewLine}");
                    textBoxOutputB.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in ProcessDataPacket: {ex.Message}{Environment.NewLine}");
                });
            }
        }

        /// <summary>
        /// Builds an 8-byte command for turning lights on.
        /// </summary>
        private byte[] BuildOnCommand(byte controllerHeader)
        {
            return new byte[]
            {
                controllerHeader,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0x0A
            };
        }

        /// <summary>
        /// Builds an 8-byte command for turning lights off.
        /// </summary>
        private byte[] BuildOffCommand(byte controllerHeader)
        {
            return new byte[]
            {
                controllerHeader,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x0A
            };
        }

        #region Outgoing Command Button Handlers (Toggle Behavior)

        private void ButtonAllOn_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Serial port not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                if (!allLightsOn)
                {
                    byte[] commandA = BuildOnCommand(0xFA);
                    byte[] commandB = BuildOnCommand(0xFB);
                    serialPort.Write(commandA, 0, commandA.Length);
                    System.Threading.Thread.Sleep(50);
                    serialPort.Write(commandB, 0, commandB.Length);
                    allLightsOn = true;
                    buttonAllOn.Content = "All Lights Off";
                    statusText.Text = "Sent: All Lights On";
                }
                else
                {
                    byte[] commandA = BuildOffCommand(0xFA);
                    byte[] commandB = BuildOffCommand(0xFB);
                    serialPort.Write(commandA, 0, commandA.Length);
                    System.Threading.Thread.Sleep(50);
                    serialPort.Write(commandB, 0, commandB.Length);
                    allLightsOn = false;
                    buttonAllOn.Content = "All Lights On";
                    statusText.Text = "Sent: All Lights Off";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending All Lights command: {ex.Message}");
            }
        }

        private void ButtonCtrlAOn_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Serial port not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                if (!ctrlAOn)
                {
                    byte[] commandA = BuildOnCommand(0xFA);
                    serialPort.Write(commandA, 0, commandA.Length);
                    ctrlAOn = true;
                    buttonCtrlAOn.Content = "Controller A Off";
                    statusText.Text = "Sent: Controller A On";
                }
                else
                {
                    byte[] commandA = BuildOffCommand(0xFA);
                    serialPort.Write(commandA, 0, commandA.Length);
                    ctrlAOn = false;
                    buttonCtrlAOn.Content = "Controller A On";
                    statusText.Text = "Sent: Controller A Off";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending Controller A command: {ex.Message}");
            }
        }

        private void ButtonCtrlBOn_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Serial port not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                if (!ctrlBOn)
                {
                    byte[] commandB = BuildOnCommand(0xFB);
                    serialPort.Write(commandB, 0, commandB.Length);
                    ctrlBOn = true;
                    buttonCtrlBOn.Content = "Controller B Off";
                    statusText.Text = "Sent: Controller B On";
                }
                else
                {
                    byte[] commandB = BuildOffCommand(0xFB);
                    serialPort.Write(commandB, 0, commandB.Length);
                    ctrlBOn = false;
                    buttonCtrlBOn.Content = "Controller B On";
                    statusText.Text = "Sent: Controller B Off";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending Controller B command: {ex.Message}");
            }
        }

        private void ButtonSendRaw_Click(object sender, RoutedEventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Serial port not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string rawHex = textBoxRawHex.Text.Trim();
            if (string.IsNullOrEmpty(rawHex))
            {
                MessageBox.Show("Please enter hex data.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                string[] tokens = rawHex.Split(new char[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                List<byte> data = new List<byte>();
                foreach (string token in tokens)
                {
                    data.Add(byte.Parse(token, NumberStyles.HexNumber));
                }
                byte[] command = data.ToArray();
                serialPort.Write(command, 0, command.Length);
                statusText.Text = $"Sent Raw: {rawHex}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending raw hex data: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the Debug Window ensuring only one instance exists.
        /// </summary>
        private void ButtonOpenDebug_Click(object sender, RoutedEventArgs e)
        {
            if (debugWindow != null && debugWindow.IsLoaded)
            {
                debugWindow.Activate();
            }
            else
            {
                debugWindow = new DebugWindow(this);
                debugWindow.Closed += (s, args) => debugWindow = null;
                debugWindow.Show();
            }
        }

        #endregion

        /// <summary>
        /// Public method to simulate incoming data (used by DebugWindow).
        /// </summary>
        public void SimulateIncomingData(byte[] data)
        {
            ProcessDataPacket(data);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (serialPort.IsOpen)
                serialPort.Close();
            base.OnClosing(e);
        }
    }
}
