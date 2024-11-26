using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SmartPlug
{
    public partial class Form1 : Form
    {
        private const string ConfigFilePath = "SmartPlugConfig.txt";
        private readonly System.Threading.Timer backgroundTimer;

        public Form1()
        {
            InitializeComponent();

            // Initialize the background timer to check every minute
            backgroundTimer = new System.Threading.Timer(CheckAndTurnOffDevices, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            // Load data into text fields on application startup
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var lines = File.ReadAllLines(ConfigFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(',');
                        if (parts.Length < 1) continue;

                        // Populate the first two IP addresses into text fields
                        if (textBox1.Text == string.Empty) textBox1.Text = parts[0];
                        else if (textBox2.Text == string.Empty) textBox2.Text = parts[0];
                        else if (textBox3.Text == string.Empty) textBox3.Text = parts[0];
                    }
                }
                else
                {
                    // Create the file with default data if it doesn't exist
                    File.WriteAllText(ConfigFilePath, "192.168.0.101\n192.168.0.102");
                    MessageBox.Show("Default configuration file created. Please update as needed.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var entries = new List<string>();

                if (!string.IsNullOrEmpty(textBox1.Text))
                    entries.Add($"{textBox1.Text},,"); // Add with placeholders for start and wake-up times

                if (!string.IsNullOrEmpty(textBox2.Text))
                    entries.Add($"{textBox2.Text},,");

                File.WriteAllLines(ConfigFilePath, entries);
                MessageBox.Show("Configuration saved successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            var wakeUpTime = currentTime.AddMinutes(int.Parse(txtMiniGolfTimer.Text)); // Adjust the duration as needed
            lblMessage.Text = ""; // Clear the message

            // Update configuration with start and wake-up times
            UpdateConfiguration(textBox1.Text, currentTime, wakeUpTime);
            UpdateConfiguration(textBox2.Text, currentTime, wakeUpTime);
            UpdateConfiguration(textBox3.Text, currentTime, wakeUpTime);
            // Turn on the smart plugs
            onff(checkBox1.Checked, textBox1.Text);
            onff(checkBox1.Checked, textBox2.Text);
            onff(checkBox1.Checked, textBox3.Text);
        }

        private void UpdateConfiguration(string ip, DateTime startTime, DateTime wakeUpTime)
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return;

                var lines = File.ReadAllLines(ConfigFilePath).ToList();
                bool found = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts[0] == ip)
                    {
                        lines[i] = $"{ip},{startTime},{wakeUpTime}";
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    lines.Add($"{ip},{startTime},{wakeUpTime}");
                }

                File.WriteAllLines(ConfigFilePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void onff(bool onoff, string ip)
        {
            try
            {
                var plug = new TPLinkSmartDevices.Devices.TPLinkSmartPlug(ip);
                plug.OutletPowered = onoff;
                string status = onoff ? "ON" : "OFF";
                Console.WriteLine($"Smart plug at {ip} is turned {status}.");
                lblMessage.Text += $"Smart plug at {ip} is turned {status}.\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to communicate with smart plug at {ip}: {ex.Message}");
                lblMessage.Text += $"Failed to communicate with smart plug at {ip}: {ex.Message}\n";
            }
        }

        private void CheckAndTurnOffDevices(object state)
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return;

                var lines = File.ReadAllLines(ConfigFilePath).ToList();
                var updatedLines = new List<string>();

                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;

                    var ip = parts[0];
                    if (!DateTime.TryParse(parts[2], out DateTime wakeUpTime)) continue;

                    if (DateTime.Now >= wakeUpTime)
                    {
                        onff(false, ip); // Turn off the smart plug
                        Console.WriteLine($"Smart plug at {ip} turned OFF.");
                    }
                    else
                    {
                        updatedLines.Add(line); // Keep entries with valid future wake-up times
                    }
                }

                File.WriteAllLines(ConfigFilePath, updatedLines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking and turning off devices: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
