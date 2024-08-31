using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private Label lblNumberOfButtons;
    private TextBox txtNumberOfButtons;
    private Label lblButtonsPerRow;
    private TextBox txtButtonsPerRow;
    private Label lblControllersCount;
    private TextBox txtControllersCount;
    private Button btnGenerateButtons;
    private Panel panelContainer;
    private ColorDialog colorDialog;
    private Color buttonColor = Color.Blue;
    private int numberOfButtons;
    private int buttonsPerRow;
    private int controllersCount;
    private List<UdpHandler> udpHandlers;
    private List<Panel> panels;
    protected Dictionary<UdpHandler, Panel> handlerDevices;

    public static readonly Dictionary<string, KnownColor> ColorMap = new Dictionary<string, KnownColor>
    {
        { "ff0000", KnownColor.Red },       // Red
        { "00ff00", KnownColor.Lime },      // Green
        { "0000ff", KnownColor.Blue },      // Blue
        { "ffff00", KnownColor.Yellow },    // Yellow
        { "000000", KnownColor.Black },     // NoColor (Black)
        { "ffc0cb", KnownColor.Pink },      // Pink
        { "00ffff", KnownColor.Cyan },      // Cyan
        { "ff00ff", KnownColor.Magenta },   // Magenta
        { "ffa500", KnownColor.Orange },    // Orange
        { "800080", KnownColor.Purple },    // Purple
        { "bfff00", KnownColor.YellowGreen }, // Lime (YellowGreen is the closest known color)
        { "008080", KnownColor.Teal },      // Teal
        { "e6e6fa", KnownColor.Lavender },  // Lavender
        { "a52a2a", KnownColor.Brown },     // Brown
        { "800000", KnownColor.Maroon },    // Maroon
        { "000080", KnownColor.Navy },      // Navy
        { "808000", KnownColor.Olive },     // Olive
        { "ff7f50", KnownColor.Coral },     // Coral
        { "ffd700", KnownColor.Gold },      // Gold
        { "c0c0c0", KnownColor.Silver },    // Silver
        { "808080", KnownColor.Gray },      // Gray
        { "ffffff", KnownColor.White }      // White
    };

    public MainForm()
    {
        InitializeComponent();
        this.Resize += MainForm_Resize;
    }

    private void MainForm_Resize(object sender, EventArgs e)
    {
       // AdjustButtons();
    }

    private void btnGenerateButtons_Click(object sender, EventArgs e)
    {
        panelContainer.Controls.Clear();

        if (int.TryParse(txtNumberOfButtons.Text, out numberOfButtons) && numberOfButtons > 0 &&
            int.TryParse(txtButtonsPerRow.Text, out buttonsPerRow) && buttonsPerRow > 0 &&
            int.TryParse(txtControllersCount.Text, out controllersCount) && controllersCount > 0)
        {
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
        else
        {
            MessageBox.Show("Please enter valid numbers for buttons, buttons per row, and controllers count.");
        }
    }

    private void GeneratePanels(int count)
    {
        panels = new List<Panel>();

        // Calculate button size and margin
        int buttonSize = 50;
        int margin = 10;
        int buttonsPerPanel = numberOfButtons / count;
        int remainingButtons = numberOfButtons % count;

        // Calculate the maximum number of rows required for any panel
        int maxRows = (int)Math.Ceiling((double)(buttonsPerPanel + (remainingButtons > 0 ? 1 : 0)) / buttonsPerRow);

        // Calculate panel width and height based on the number of buttons per row and the maximum number of rows
        int panelWidth = (buttonsPerRow * (buttonSize + margin)) - margin + 20;
        int panelHeight = (maxRows * (buttonSize + margin)) - margin + 20;

        for (int i = 0; i < count; i++)
        {
            Panel panel = new Panel
            {
                Size = new Size(panelWidth, panelHeight+5),
                Location = new Point(10, i * (panelHeight + 10) + 10),
                BorderStyle = BorderStyle.FixedSingle
                
            };
            panelContainer.Width= (panelWidth)+100;
            panelContainer.Height = (panelHeight*(count + 1))+100;
            panelContainer.BorderStyle = BorderStyle.FixedSingle;
            panelContainer.BackColor = Color.DarkGray;
            panelContainer.Controls.Add(panel);
            panels.Add(panel);
        }
    }


    protected void StartReceivingMessages()
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

            if (ColorMap.TryGetValue(hexColor, out KnownColor knownColor))
            {
                handlerDevices[handler].Controls[i / 3].BackColor = Color.FromKnownColor(knownColor);
            }
            else
            {
                logger.Log($"Color {hexColor} not found in ColorMap.");
            }
        }
    }

    private void btnChooseColor_Click(object sender, EventArgs e)
    {
        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            buttonColor = colorDialog.Color;
        }
    }

    private void GenerateButtons(Panel panel, int numberOfButtons, int buttonsPerRow)
    {
        int buttonSize = 50;
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

    private void AdjustButtons()
    {
        if (numberOfButtons > 0 && buttonsPerRow > 0 && panels != null)
        {
            int buttonsPerPanel = numberOfButtons / panels.Count;
            int remainingButtons = numberOfButtons % panels.Count;

            for (int i = 0; i < panels.Count; i++)
            {
                int buttonCount = buttonsPerPanel + (i < remainingButtons ? 1 : 0);
                panels[i].Controls.Clear();
                GenerateButtons(panels[i], buttonCount, buttonsPerRow);
            }
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

    private void panelContainer_Paint(object sender, PaintEventArgs e)
    {

    }
}
