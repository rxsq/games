using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace DoorLock
{
    public partial class Form1 : Form
    {
        private LockController lockController;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Populate available COM ports
            string[] ports = SerialPort.GetPortNames();
            cmbComPorts.Items.AddRange(ports);

            if (ports.Length > 0)
            {
                cmbComPorts.SelectedIndex = 0; // Select the first available port by default
            }
        }

        private void btnOpenDoor_Click(object sender, EventArgs e)
        {
            if (cmbComPorts.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (lockController == null)
                {
                    lockController = new LockController(cmbComPorts.SelectedItem.ToString());
                }

                lockController.TurnRelayOn();
                MessageBox.Show("Door opened successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the door: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCloseDoor_Click(object sender, EventArgs e)
        {
            if (cmbComPorts.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (lockController == null)
                {
                    lockController = new LockController(cmbComPorts.SelectedItem.ToString());
                }

                lockController.TurnRelayOff();
                MessageBox.Show("Door closed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to close the door: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && lockController != null)
            {
                lockController.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
