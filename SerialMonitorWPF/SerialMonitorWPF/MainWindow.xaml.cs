using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SerialMonitorWPF
{
    public partial class MainWindow : Window
    {
        private SerialPort serialPort = new SerialPort();
        private int numberOfLasersPerController = 48;
        public List<int> activeDevices = new List<int>();
        private const byte HEADER_BYTE = 0xCB;
        private const byte FOOTER_BYTE = 0x0A;

        public MainWindow()
        {
            InitializeComponent();
            LoadPorts();
            buttonRefresh.Click += (s, e) => LoadPorts();
            buttonConnect.Click += ButtonConnect_Click;
            serialPort.DataReceived += SerialPort_DataReceived;
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
                // Wait a small amount of time to ensure all data has arrived
                System.Threading.Thread.Sleep(50);

                int bytesToRead = serialPort.BytesToRead;
                if (bytesToRead < 6) // We need at least 6 bytes for 48 sensors
                {
                    return; // Not enough data yet
                }

                byte[] receivedPacket = new byte[bytesToRead];
                int bytesRead = serialPort.Read(receivedPacket, 0, bytesToRead);

                if (bytesRead >= 6) // Only process if we have enough data
                {
                    ProcessDataPacket(receivedPacket);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    textBoxOutput.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}{Environment.NewLine}");
                });
            }
        }


        private List<int> GetCutLasers(byte[] message)
        {
            List<int> cutLasers = new List<int>();

            try
            {
                // Validate message format
                if (message == null || message.Length != 8)  // Must be exactly 8 bytes
                {
                    throw new ArgumentException($"Invalid message length: {message?.Length ?? 0}, expected 8 bytes");
                }

                // Verify header and footer
                if (message[0] != HEADER_BYTE || message[7] != FOOTER_BYTE)
                {
                    throw new ArgumentException($"Invalid message format. Header: 0x{message[0]:X2}, Footer: 0x{message[7]:X2}");
                }

                // Process only the 6 data bytes (indexes 1-6)
                for (int byteIndex = 1; byteIndex < 7; byteIndex++)
                {
                    byte currentByte = message[byteIndex];
                    // Process each bit in the byte, from LSB to MSB (reversed order)
                    for (int bitIndex = 0; bitIndex <= 7; bitIndex++)  // Changed to process from LSB to MSB
                    {
                        int bit = (currentByte >> bitIndex) & 0x1;
                        cutLasers.Add(bit);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCutLasers: {ex.Message}");
                // Ensure we return a valid list even in case of error
                cutLasers.Clear();
            }

            // Ensure we always return exactly 48 values
            while (cutLasers.Count < 48)
            {
                cutLasers.Add(0);
            }

            return cutLasers;
        }

        private void ProcessDataPacket(byte[] data)
        {
            try
            {
                List<int> cutLasers = GetCutLasers(data);

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        int rows = 8, cols = 6;
                        if (!int.TryParse(textBoxRows.Text, out rows))
                            rows = 8;
                        if (!int.TryParse(textBoxCols.Text, out cols))
                            cols = 6;

                        StringBuilder displayBuilder = new StringBuilder();
                        displayBuilder.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sensor Grid Data:");

                        // Add hex representation of received bytes
                        displayBuilder.Append("Raw Data (HEX): ");
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (i == 0)
                                displayBuilder.Append($"[{data[i]:X2}] "); // Header
                            else if (i == data.Length - 1)
                                displayBuilder.Append($"[{data[i]:X2}]"); // Footer
                            else
                                displayBuilder.Append($"{data[i]:X2} "); // Data bytes
                        }
                        displayBuilder.AppendLine();

                        // Add binary representation of data bytes only
                        displayBuilder.AppendLine("\nData Bytes to Binary conversion:");
                        for (int i = 1; i < 7; i++) // Only process bytes 1-6
                        {
                            displayBuilder.Append($"Byte {i} (0x{data[i]:X2}): ");
                            for (int bit = 7; bit >= 0; bit--)
                            {
                                displayBuilder.Append((data[i] >> bit) & 0x1);
                            }
                            displayBuilder.AppendLine();
                        }

                        displayBuilder.AppendLine("----------------------------------------");

                        // Display grid
                        displayBuilder.Append("   ");
                        for (int c = 0; c < cols; c++)
                        {
                            displayBuilder.Append($"{c,3} ");
                        }
                        displayBuilder.AppendLine();

                        for (int r = 0; r < rows; r++)
                        {
                            displayBuilder.Append($"{r,2} ");
                            for (int c = 0; c < cols; c++)
                            {
                                int index = r * cols + c;
                                if (index < cutLasers.Count)
                                {
                                    displayBuilder.Append($"{cutLasers[index],3} ");
                                }
                                else
                                {
                                    displayBuilder.Append("  - ");
                                }
                            }
                            displayBuilder.AppendLine();
                        }

                        displayBuilder.AppendLine("----------------------------------------");
                        displayBuilder.AppendLine($"Total Sensors: {cutLasers.Count}");
                        displayBuilder.AppendLine($"Active Sensors: {cutLasers.Count(x => x > 0)}");
                        displayBuilder.AppendLine($"Grid Size: {rows}x{cols}");
                        displayBuilder.AppendLine($"Message Format: Header[0xCB] + 6 Data Bytes + Footer[0x0A]");
                        displayBuilder.AppendLine();

                        if (textBoxOutput.Text.Length > 5000)
                        {
                            textBoxOutput.Text = textBoxOutput.Text.Substring(textBoxOutput.Text.Length - 4000);
                        }
                        textBoxOutput.AppendText(displayBuilder.ToString());
                        textBoxOutput.ScrollToEnd();
                    }
                    catch (Exception ex)
                    {
                        textBoxOutput.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in UI update: {ex.Message}{Environment.NewLine}");
                        textBoxOutput.ScrollToEnd();
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    textBoxOutput.AppendText($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR in ProcessDataPacket: {ex.Message}{Environment.NewLine}");
                    textBoxOutput.ScrollToEnd();
                });
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            base.OnClosing(e);
        }
    }
}
