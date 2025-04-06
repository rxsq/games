namespace Simulator.Forms
{
    partial class LaserSimulator
    {
        private void InitializeComponent()
        {
            this.btnConfig = new Button();
            this.panelContainer = new FlowLayoutPanel();
            this.SuspendLayout();

            // btnConfig
            this.btnConfig.Location = new Point(13, 13);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Size = new Size(100, 23);
            this.btnConfig.TabIndex = 0;
            this.btnConfig.Text = "Config";
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new EventHandler(this.btnConfig_Click);

            // panelContainer
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.Size = new Size(1920, 900); // Adjusted size
            this.panelContainer.FlowDirection = FlowDirection.LeftToRight;
            this.panelContainer.WrapContents = false;
            this.panelContainer.TabIndex = 1;
            this.panelContainer.AutoScroll = true;
            this.panelContainer.Padding = new Padding(0, 0, 0, 50); // Added padding at the bottom

            // DefaultForm
            this.AutoScroll = true;
            this.Controls.Add(this.btnConfig);
            this.Controls.Add(this.panelContainer);
            this.Name = "DefaultForm";
            this.Size = new Size(1920, 1080);
            this.ResumeLayout(false);

            // Adjust the layout and ensure the panelContainer is centered and resizes with the form
            this.Resize += new EventHandler(this.DefaultForm_Resize);
        }

        private void DefaultForm_Resize(object sender, EventArgs e)
        {
            // Center the panelContainer within the DefaultForm
            panelContainer.Left = (this.ClientSize.Width - panelContainer.Width) / 2;
            panelContainer.Top = (this.ClientSize.Height - panelContainer.Height) / 2;
        }
        private Button btnConfig;
        private FlowLayoutPanel panelContainer;
        private Panel playersContainer;
        private Label lblNumberOfButtons;
        private TextBox txtNumberOfButtons;
        private Label lblButtonsPerRow;
        private TextBox txtButtonsPerRow;
        private Label lblControllersCount;
        private TextBox txtControllersCount;
        private Button btnGenerateButtons;
        private ColorDialog colorDialog;
    }
}
