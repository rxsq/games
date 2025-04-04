using System.Windows.Forms;

namespace Simulator.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            InitializeTabs();
        }
        private void InitializeTabs()
        {
            var simpleTab = new TabPage("Simple Configuration");
            simpleTab.Controls.Add(new DefaultForm { Dock = DockStyle.Fill });

            var customTab = new TabPage("Custom Controller Config");
            customTab.Controls.Add(new CustomConfigForm { Dock = DockStyle.Fill });
        }
    }
}
