using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SerialMonitorWPF
{
    public enum SimulationMode
    {
        None,
        AllOn,
        CtrlA,
        CtrlB
    }

    public partial class DebugWindow : Window
    {
        private MainWindow mainWindow;
        private const byte FOOTER_BYTE = 0x0A; 

        private DispatcherTimer simulationTimer;
        private SimulationMode currentSimulationMode = SimulationMode.None;

        private DispatcherTimer gridSimulationTimer;

        public DebugWindow(MainWindow mainWnd)
        {
            InitializeComponent();
            mainWindow = mainWnd;

            for (int i = 0; i < 48; i++)
            {
                CheckBox cb = new CheckBox();
                cb.Content = i.ToString();
                cb.Margin = new Thickness(2);
                ugSensorGrid.Children.Add(cb);
            }

            simulationTimer = new DispatcherTimer();
            simulationTimer.Interval = TimeSpan.FromMilliseconds(500);
            simulationTimer.Tick += SimulationTimer_Tick;

            gridSimulationTimer = new DispatcherTimer();
            gridSimulationTimer.Interval = TimeSpan.FromMilliseconds(500);
            gridSimulationTimer.Tick += GridSimulationTimer_Tick;
        }

        /// <summary>
        /// Timer tick for continuous simulation (Raw Data tab).
        /// </summary>
        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (currentSimulationMode == SimulationMode.None)
                return;

            switch (currentSimulationMode)
            {
                case SimulationMode.AllOn:
                    mainWindow.SimulateIncomingData(BuildSimulationPacket(0xCA));
                    mainWindow.SimulateIncomingData(BuildSimulationPacket(0xCB));
                    break;
                case SimulationMode.CtrlA:
                    mainWindow.SimulateIncomingData(BuildSimulationPacket(0xCA));
                    break;
                case SimulationMode.CtrlB:
                    mainWindow.SimulateIncomingData(BuildSimulationPacket(0xCB));
                    break;
            }
        }

        /// <summary>
        /// Builds an 8-byte simulation packet with the given header and 6 data bytes (all 0xFF), ending with a footer.
        /// Used for continuous simulation in the Raw Data tab.
        /// </summary>
        private byte[] BuildSimulationPacket(byte header)
        {
            return new byte[]
            {
                header,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                FOOTER_BYTE
            };
        }

        /// <summary>
        /// One-time simulation of raw hex data.
        /// </summary>
        private void btnSimulateRaw_Click(object sender, RoutedEventArgs e)
        {
            string raw = txtRawData.Text.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                MessageBox.Show("Please enter raw hex data.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                string[] tokens = raw.Split(new char[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                List<byte> data = new List<byte>();
                foreach (string token in tokens)
                    data.Add(byte.Parse(token, NumberStyles.HexNumber));
                mainWindow.SimulateIncomingData(data.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error simulating raw data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Builds a packet from the Visual Grid's current state.
        /// </summary>
        private byte[] BuildGridPacket()
        {
            byte header = 0xCA; 
            if (cmbController.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString().Contains("B"))
                    header = 0xCB;
            }

            byte[] packet = new byte[8];
            packet[0] = header;
            for (int group = 0; group < 6; group++)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int index = group * 8 + bit;
                    if (index < ugSensorGrid.Children.Count)
                    {
                        if (ugSensorGrid.Children[index] is CheckBox cb && cb.IsChecked == true)
                            b |= (byte)(1 << bit);
                    }
                }
                packet[group + 1] = b;
            }
            packet[7] = FOOTER_BYTE;
            return packet;
        }

        /// <summary>
        /// One-time simulation of grid data.
        /// </summary>
        private void btnSimulateGrid_Click(object sender, RoutedEventArgs e)
        {
            byte[] packet = BuildGridPacket();
            mainWindow.SimulateIncomingData(packet);
        }

        /// <summary>
        /// Event handler for the Toggle Select All button.
        /// If all checkboxes are selected, it clears them; otherwise, it selects them all.
        /// </summary>
        private void btnToggleSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool allSelected = true;
            foreach (var child in ugSensorGrid.Children)
            {
                if (child is CheckBox cb)
                {
                    if (cb.IsChecked != true)
                    {
                        allSelected = false;
                        break;
                    }
                }
            }
            bool newState = !allSelected;
            foreach (var child in ugSensorGrid.Children)
            {
                if (child is CheckBox cb)
                {
                    cb.IsChecked = newState;
                }
            }
        }

        private void btnSimAllOn_Click(object sender, RoutedEventArgs e)
        {
            currentSimulationMode = SimulationMode.AllOn;
            simulationTimer.Start();
        }

        private void btnSimCtrlA_Click(object sender, RoutedEventArgs e)
        {
            currentSimulationMode = SimulationMode.CtrlA;
            simulationTimer.Start();
        }

        private void btnSimCtrlB_Click(object sender, RoutedEventArgs e)
        {
            currentSimulationMode = SimulationMode.CtrlB;
            simulationTimer.Start();
        }

        private void btnStopContinuous_Click(object sender, RoutedEventArgs e)
        {
            simulationTimer.Stop();
            currentSimulationMode = SimulationMode.None;
        }

        private void btnStartContinuousGrid_Click(object sender, RoutedEventArgs e)
        {
            gridSimulationTimer.Start();
        }

        private void btnStopContinuousGrid_Click(object sender, RoutedEventArgs e)
        {
            gridSimulationTimer.Stop();
        }

        private void GridSimulationTimer_Tick(object sender, EventArgs e)
        {
            byte[] packet = BuildGridPacket();
            mainWindow.SimulateIncomingData(packet);
        }
    }
}
