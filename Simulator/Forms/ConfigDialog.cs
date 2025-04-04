using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulator.Forms
{
    public partial class ConfigDialog : Form
    {
        public int NumberOfButtons { get; private set; }
        public int ButtonsPerRow { get; private set; }
        public int ControllersCount { get; private set; }
        public ConfigDialog()
        {
            InitializeComponent();
        }
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtNumberOfButtons.Text, out int numberOfButtons) && numberOfButtons > 0 &&
                int.TryParse(txtButtonsPerRow.Text, out int buttonsPerRow) && buttonsPerRow > 0 &&
                int.TryParse(txtControllersCount.Text, out int controllersCount) && controllersCount > 0)
            {
                NumberOfButtons = numberOfButtons;
                ButtonsPerRow = buttonsPerRow;
                ControllersCount = controllersCount;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for all fields.");
            }
        }
    }
}
