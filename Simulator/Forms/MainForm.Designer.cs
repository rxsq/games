namespace Simulator.Forms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private TabControl tabControl;
        private TabPage tabDefaultConfig;
        private TabPage tabCustomConfig;
        private TabPage tabLaserSimulator;
        private DefaultForm defaultConfigForm;
        private CustomConfigForm customConfigForm;
        private LaserSimulator laserSimulatorForm;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl = new TabControl();
            this.tabDefaultConfig = new TabPage();
            this.tabCustomConfig = new TabPage();
            this.tabLaserSimulator = new TabPage();
            this.defaultConfigForm = new DefaultForm();
            this.customConfigForm = new CustomConfigForm();
            this.laserSimulatorForm = new LaserSimulator();

            // MainForm
            this.ClientSize = new Size(1920, 1080);
            this.Text = "Main Form";

            // tabControl
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Controls.Add(this.tabDefaultConfig);
            this.tabControl.Controls.Add(this.tabCustomConfig);
            this.tabControl.Controls.Add(this.tabLaserSimulator);

            // tabDefaultConfig
            this.tabDefaultConfig.Text = "Default Config";
            this.defaultConfigForm.Dock = DockStyle.Fill; // Ensure resizing
            this.defaultConfigForm.AutoScroll = true; // Enable scrolling
            this.tabDefaultConfig.Controls.Add(this.defaultConfigForm);

            // tabCustomConfig
            this.tabCustomConfig.Text = "Custom Config";
            this.customConfigForm.Dock = DockStyle.Fill; // Ensure resizing
            this.customConfigForm.AutoScroll = true; // Enable scrolling if needed
            this.tabCustomConfig.Controls.Add(this.customConfigForm);

            // tabLaserSimulator
            this.tabLaserSimulator.Text = "Laser Simulator";
            this.laserSimulatorForm.Dock = DockStyle.Fill; // Ensure resizing
            this.laserSimulatorForm.AutoScroll = true; // Enable scrolling if needed
            this.tabLaserSimulator.Controls.Add(this.laserSimulatorForm);

            // MainForm
            this.Controls.Add(this.tabControl);
        }

        #endregion
    }
}