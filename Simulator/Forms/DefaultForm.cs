using Simulator.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Simulator.Helpers;

namespace Simulator.Forms
{
    public partial class DefaultForm : UserControl
    {
        private Color buttonColor = Color.Blue;
        private int numberOfButtons;
        private int buttonsPerRow;
        private int controllersCount;
        private List<UdpHandler> udpHandlers;
        private List<Panel> panels;
        private Dictionary<UdpHandler, Panel> handlerDevices;
        int panelWidth = 0;

        public DefaultForm()
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

            // Dispose of existing UDP handlers
            if (udpHandlers != null)
            {
                foreach (var handler in udpHandlers)
                {
                    handler.StopReceiving(); // Or handler.Close(); depending on your implementation
                }
                udpHandlers.Clear();
            }

            handlerDevices?.Clear();

            GeneratePanels(controllersCount);
            int buttonsPerPanel = numberOfButtons / controllersCount;
            int remainingButtons = numberOfButtons % controllersCount;

            udpHandlers = new List<UdpHandler>();
            handlerDevices = new Dictionary<UdpHandler, Panel>();

            for (int i = 0; i < controllersCount; i++)
            {
                int buttonCount = buttonsPerPanel + (i < remainingButtons ? 1 : 0);
                GenerateButtons(panels[i], buttonCount, buttonsPerRow);

                var udpSender = new UdpHandler("127.0.0.1", 7113 + i, 21 + i, 20105 + i, getMessage(2000, panels[i]));
                udpHandlers.Add(udpSender);
                handlerDevices.Add(udpSender, panels[i]);
            }

            StartReceivingMessages();
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
            panelWidth = (buttonsPerRow * (buttonSize + margin)) - margin + 50;
            int panelHeight = (maxRows * (buttonSize + margin)) - margin + 50;

            for (int i = 0; i < count; i++)
            {
                Panel panel = new Panel
                {
                    Size = new Size(panelWidth, panelHeight + 5),
                    Location = new Point((panelContainer.Width - panelWidth) / 2, i * (panelHeight + 10)),
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(5),
                };

                panelContainer.Controls.Add(panel);
                panels.Add(panel);
            }

            // Center the panelContainer
            int paddingLeft = (panelContainer.ClientSize.Width - panelWidth + 20) / 2;
            panelContainer.Padding = new Padding(paddingLeft, 0, 0, 0);
        }

        private void StartReceivingMessages()
        {
            foreach (var handler in udpHandlers)
            {
                handler.BeginReceive(data => ReceiveCallback(data, handler));
            }
        }

        private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
        {
            string receivedData = BitConverter.ToString(receivedBytes);
            var colors = receivedData.Split('-').ToList<string>();
            logger.Log($"Received data: {receivedData}");
            ProcessGameResponse(receivedData.Substring(6), handler);
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

        private void ProcessGameResponse(string response, UdpHandler handler)
        {
            var hexValues = response.Replace("FE", "FF").Split('-').Select(v => v.ToLower()).ToList();

            for (int i = 0; i < hexValues.Count; i += 3)
            {
                string hexColor = string.Concat(hexValues[i], hexValues[i + 1], hexValues[i + 2]);

                if (ColorMapper.ColorMap.TryGetValue(hexColor, out KnownColor knownColor))
                {
                    handlerDevices[handler].Controls[i / 3].BackColor = Color.FromKnownColor(knownColor);
                }
                else
                {
                    logger.Log($"Color {hexColor} not found in ColorMap.");
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
                int row = i / buttonsPerRow;
                int column = (row % 2 == 1) ? buttonsPerRow - 1 - (i % buttonsPerRow) : i % buttonsPerRow;
                int x = startX + column * (buttonSize + margin);
                int y = startY + row * (buttonSize + margin);

                Button button = new Button
                {
                    Size = new Size(buttonSize, buttonSize),
                    Location = new Point(x, y),
                    BackColor = buttonColor,
                    Text = (i).ToString(),
                    Name = $"target{i}",
                    Tag = i,
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

            var udpSender = handlerDevices.FirstOrDefault(kv => kv.Value == parentPanel).Key;

            udpSender?.SendAsync(getMessage(buttonNumber, parentPanel));
        }

        private string getMessage(int buttonNumber, Control pnl)
        {
            int count = pnl.Controls.Count;
            StringBuilder sb = new StringBuilder("FC06");

            for (int i = 0; i < count; i++)
            {
                if (i == buttonNumber)
                {
                    sb.Append("0A");
                }
                else
                {
                    sb.Append("05");
                }
            }
            logger.Log($"Button number: {buttonNumber} - {sb}");
            return sb.ToString();
        }

        

    }
}