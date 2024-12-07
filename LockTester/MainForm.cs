using System;
using System.Windows.Forms;

namespace LockTester
{
    public partial class MainForm : Form
    {
        private LockController lockController;

        public MainForm()
        {
            InitializeComponent();
            lblStatus.Text = "Status: Not connected";
            btnRelayOn.Enabled = false;
            btnRelayOff.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string portName = txtComPort.Text.Trim();
            if (string.IsNullOrEmpty(portName))
            {
                lblStatus.Text = "Error: Please enter a COM port.";
                return;
            }

            try
            {
                lockController = new LockController(portName);
                lblStatus.Text = $"Connected to {portName}.";
                btnRelayOn.Enabled = true;
                btnRelayOff.Enabled = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Failed to connect: {ex.Message}";
            }
        }

        private void btnRelayOn_Click(object sender, EventArgs e)
        {
            if (lockController != null)
            {
                try
                {
                    lockController.TurnRelayOn();
                    lblStatus.Text = "Relay turned ON.";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Error turning relay ON: {ex.Message}";
                }
            }
            else
            {
                lblStatus.Text = "Error: LockController is not initialized.";
            }
        }

        private void btnRelayOff_Click(object sender, EventArgs e)
        {
            if (lockController != null)
            {
                try
                {
                    lockController.TurnRelayOff();
                    lblStatus.Text = "Relay turned OFF.";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Error turning relay OFF: {ex.Message}";
                }
            }
            else
            {
                lblStatus.Text = "Error: LockController is not initialized.";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Dispose of the LockController when closing the form
            lockController?.Dispose();
        }
    }
}
