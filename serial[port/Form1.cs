using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace serial_port
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Define the serial port settings
            string portName = "COM3"; // Replace with the actual COM port of your ESP32
            int baudRate = 115200;    // Make sure the baud rate matches the one used by the ESP32

            // Create and configure the SerialPort object
            SerialPort serialPort = new SerialPort(portName, baudRate);

            try
            {
                // Open the serial port
                serialPort.Open();
                lblMsg.Text= ($"Connected to ESP32 on port {portName}");

                // Send the "START" message to the ESP32
                string message = "START";
                serialPort.WriteLine(message);
                lblMsg.Text=($"Message sent: {message}");

                // Wait for a brief moment in case ESP32 sends a response
                System.Threading.Thread.Sleep(2000);
                //while (true)
                {
                    // Check for and read any response from the ESP32
                    if (serialPort.BytesToRead > 0)
                    {
                        string response = serialPort.ReadExisting();
                        lblMsg.Text = ($"Response from ESP32: {response}");
                        if(response.Contains("GAME_END") )
                        {
                         //   lblStatus.Text = ($"Received END message. Closing connection.");
                        //    break;
                        }

                        //break;
                    }
                }
              //  lblMsg.Text = "out of the loop";
            }
            catch (Exception ex)
            {
                lblStatus.Text=($"Error: {ex.Message}");
            }
            finally
            {
                // Close the serial port
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    lblStatus.Text=("Connection closed.");
                }
            }

          //  lblMsg.Text=("Press any key to exit.");
            //Console.ReadKey();
        
    }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
