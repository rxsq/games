using Simulator.Helpers;
using Simulator.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulator.Forms
{
    public partial class LaserSimulator : UserControl
    {
        private Color onColor = Color.Green;
        private Color offColor = Color.Black;
        private int numberOfButtons;
        private int buttonsPerRow;
        private int controllersCount;
        private SerialPort serialPort;
        private List<Panel> panels;
        private Dictionary<Panel, SerialPort> handlerDevices;

        public LaserSimulator()
        {
            InitializeComponent();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            using (var configDialog = new ConfigDialog())
            {
                if (configDialog.ShowDialog() == DialogResult.OK)
                {
                    numberOfButtons = configDialog.NumberOfButtons;
                    buttonsPerRow = configDialog.ButtonsPerRow;
                    controllersCount = configDialog.ControllersCount;
                    GenerateButtons();
                }
            }
        }

        private void GenerateButtons()
        {
            // Clear UI
            panelContainer.Controls.Clear();

            // Dispose of existing SerialPorts
            if (serialPort != null)
            {
                serialPort.Close();
            }

            handlerDevices?.Clear();

            GeneratePanels(controllersCount);
            int buttonsPerPanel = numberOfButtons / controllersCount;
            int remainingButtons = numberOfButtons % controllersCount;
            handlerDevices = new Dictionary<Panel, SerialPort>();
            serialPort = new SerialPort("COM111", 115200, Parity.None, 8, StopBits.One);
            serialPort.DataReceived += (s, e) => ReceiveCallback(serialPort);
            serialPort.Open();
            for (int i = 0; i < controllersCount; i++)
            {
                int buttonCount = buttonsPerPanel + (i < remainingButtons ? 1 : 0);
                GenerateButtons(panels[i], buttonCount, buttonsPerRow);

                try
                {
                    
                    handlerDevices.Add(panels[i], serialPort);
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Access to the port 'COM111' is denied: {ex.Message}");
                    // Handle the exception (e.g., log it, retry, etc.)
                }
            }
        }

        private void GeneratePanels(int count)
        {
            panels = new List<Panel>();

            // Calculate button size and margin
            int buttonSize = Math.Min(Math.Max(30, panelContainer.ClientSize.Width / (buttonsPerRow + 2)), 60);
            int margin = 10;
            int buttonsPerPanel = numberOfButtons / count;
            int remainingButtons = numberOfButtons % count;

            // Calculate the maximum number of rows required for any panel
            int maxRows = (int)Math.Ceiling((double)(buttonsPerPanel + (remainingButtons > 0 ? 1 : 0)) / buttonsPerRow);

            // Calculate panel width and height based on the number of buttons per row and the maximum number of rows
            int panelWidth = (buttonsPerRow * (buttonSize + margin)) - margin + 50;
            int panelHeight = (maxRows * (buttonSize + margin)) - margin + 50;

            for (int i = 0; i < count; i++)
            {
                Panel panel = new Panel
                {
                    TabIndex = i,
                    Size = new Size(panelWidth, panelHeight + 5),
                    Location = new Point((panelContainer.Width - panelWidth) / 2, i * (panelHeight + 10)),
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(10),
                    Margin = new Padding(10),
                };

                panelContainer.Controls.Add(panel);
                panels.Add(panel);
            }

            // Adjust the padding of the FlowLayoutPanel to center the child panels
            int totalPanelWidth = count * (panelWidth + 20); // 20 is the sum of left and right margins
            int paddingLeft = (panelContainer.ClientSize.Width - totalPanelWidth) / 2;
            panelContainer.Padding = new Padding(paddingLeft, 0, 0, 0);
        }

        private void ReceiveCallback(SerialPort serialPort)
        {
            int bytesToRead = serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            serialPort.Read(buffer, 0, bytesToRead);

            ProcessGameResponse(buffer, serialPort);
        }

        private void ProcessGameResponse(byte[] data, SerialPort serialPort)
        {
            // Check if the response has at least a header, footer, and one data byte
            if (data.Length != Math.Ceiling((numberOfButtons/panels.Count)/8.0)+2)
            {
                logger.Log("Invalid response length.");
                return;
            }

            // Extract the header and footer
            byte header = data[0];
            byte footer = data[data.Length - 1];

            // Validate the footer
            if (footer != 0x0A)
            {
                logger.Log("Invalid footer.");
                return;
            }

            // Determine the panel based on the header
            Panel panel = null;
            switch (header)
            {
                case 0xFA:
                    panel = handlerDevices.FirstOrDefault(kv => kv.Value == serialPort).Key;
                    break;
                case 0xFB:
                    panel = handlerDevices.Skip(1).FirstOrDefault(kv => kv.Value == serialPort).Key;
                    break;
                // Add more cases if you have more headers
                default:
                    logger.Log("Unknown header.");
                    return;
            }

            if (panel == null)
            {
                logger.Log("Panel not found for the given serial port.");
                return;
            }

            // Process the middle bytes to update button states
            for (int i = 1; i < data.Length - 1; i++)
            {
                byte byteData = data[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    int buttonIndex = (i - 1) * 8 + bit;
                    if (buttonIndex < panel.Controls.Count)
                    {
                        Button button = panel.Controls[buttonIndex] as Button;
                        if (button != null)
                        {
                            bool isOn = (byteData & (1 << bit)) != 0;
                            button.BackColor = isOn ? onColor : offColor;
                        }
                    }
                }
            }
        }

        private void GenerateButtons(Panel panel, int numberOfButtons, int buttonsPerRow)
        {
            int buttonSize = Math.Max(30, panel.ClientSize.Width / (buttonsPerRow + 2));
            int margin = 10;
            int totalWidth = buttonsPerRow * (buttonSize + margin) - margin;
            int totalHeight = (int)Math.Ceiling((double)numberOfButtons / buttonsPerRow) * (buttonSize + margin) - margin;

            int startX = (panel.Width - totalWidth) / 2;
            int startY = (panel.Height - totalHeight) / 2;

            for (int i = 0; i < numberOfButtons; i++)
            {
                int column = i / (numberOfButtons / buttonsPerRow);
                int row = (numberOfButtons / buttonsPerRow) - 1 - (i % (numberOfButtons / buttonsPerRow));
                int x = startX + column * (buttonSize + margin);
                int y = startY + row * (buttonSize + margin);

                Button button = new Button
                {
                    Size = new Size(buttonSize, buttonSize),
                    Location = new Point(x, y),
                    Text = (i+panel.TabIndex*numberOfButtons).ToString(),
                    Name = $"target{i}",
                    Tag = i,
                    BackColor = offColor,
                    ForeColor = Color.White
                };

                button.Click += Button_Click;

                panel.Controls.Add(button);
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            int buttonNumber = (int)button.Tag;
            var parentPanel = button.Parent as Panel;

            var serialPort = handlerDevices[parentPanel];
            byte[] message = getMessage(buttonNumber, parentPanel);
            serialPort?.Write(message, 0 , message.Count());
        }

        private byte[] getMessage(int buttonNumber, Panel pnl)
        {
            int count = pnl.Controls.Count;
            int bytesNeeded = (int)Math.Ceiling(count / 8.0);
            byte[] message = new byte[bytesNeeded + 2]; // +1 for the header

            // Determine the header based on the controller index
            int controllerIndex = handlerDevices.Keys.ToList().IndexOf(pnl);
            message[0] = (byte)(0xCA + controllerIndex);
            message[message.Length - 1] = 0x0A; // End byte

            // Construct the middle bytes
            for (int i = 0; i < count; i++)
            {
                int byteIndex = (i / 8) + 1;
                int bitIndex = i % 8;

                if (i == buttonNumber)
                {
                    // Set the respective bit to 0 (button pressed)
                    message[byteIndex] &= (byte)~(1 << bitIndex);
                }
                else
                {
                    // Set the respective bit to 1 (button not pressed)
                    message[byteIndex] |= (byte)(1 << bitIndex);
                }
            }

            logger.Log($"Button number: {buttonNumber} - {BitConverter.ToString(message)}");
            return message;
        }
    }
}