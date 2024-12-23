using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace serial_port
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            try
            {
                LaserController();
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.WriteLine($"OS: {Environment.OSVersion}");
                Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
                Console.WriteLine($"PlatformNotSupportedException: {ex.Message}");
            }
        }
        private static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex string cannot be null or empty.", nameof(hex));

            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string length must be a multiple of 2.", nameof(hex));

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }


        private static void LaserController()
        {
            string portName = "COM7"; // Replace with your port name
            int baudRate = 115200;

            SerialPort serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
            };

            try
            {
                // Subscribe to the DataReceived event
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open(); // Open the serial port
                //byte[] b = HexStringToByteArray("080A"); //start scanning
                //serialPort.Write(b, 0, b.Length);
                // b = HexStringToByteArray("02" + (33).ToString("X") + "0A"); //turn off the light
                //byte[] b = HexStringToByteArray("040A"); //for identification

                //byte[] b = HexStringToByteArray("070A"); //to light the lasers
                //b = HexStringToByteArray("060A"); //to turn off all the lasers
                //b = HexStringToByteArray("01220A");// light up first light of the first controller -> 33 in dec and 22 in hex
                //serialPort.Write(b, 0, b.Length);
                //for (int i = 33; i < 34; i++)
                //{
                //    b = HexStringToByteArray($"01{i:X2}0A");
                //    serialPort.Write(b, 0, b.Length);
                //    Thread.Sleep(100);
                //}

                TurnOffAllTheLasers(serialPort);
                Thread.Sleep(100);
                StartScanning(serialPort);
                Thread.Sleep(100);
                ConnectionRequest(serialPort);
                Thread.Sleep(100);
                TurnOnTheLaserWithScanning(serialPort, 0);
                Thread.Sleep(100);
                TurnOnTheLaserWithScanning(serialPort, 24);
                Thread.Sleep(100);
                //TurnOnAllTheLasers(serialPort);
                //Thread.Sleep(100);
                //while (true)
                //{
                //    //TurnOnAllTheLasers(serialPort);
                //    //Thread.Sleep(500);
                //    //TurnOffAllTheLasers(serialPort );
                //    //Thread.Sleep(50);
                //    //TurnOnAllTheLasers(serialPort);
                //    //Thread.Sleep(500);
                //    //ConnectionRequest(serialPort);
                //    //Thread.Sleep(5000);


                //    //TurnOffTheLaser(serialPort, 0);
                //    //Thread.Sleep(500);
                //}


                Thread.Sleep(9999000);
                Console.WriteLine($"Serial port {portName} opened successfully. Press any key to close.");
                Console.Read(); // Wait for user input to close
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open serial port: {ex.Message}");
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    Console.WriteLine($"Serial port {portName} closed.");
                }
            }
        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            try
            {
                // Read the bytes from the SerialPort buffer
                int byteCount = sp.BytesToRead;
                byte[] receivedBytes = new byte[byteCount];
                sp.Read(receivedBytes, 0, byteCount);
                if (receivedBytes != null && receivedBytes[0]==5)
                {
                    GetControllers(receivedBytes);
                }
                
                int laserno = receivedBytes[1] - 33 -23;
                int numberOfDevices = receivedBytes[2] - receivedBytes[1];
                // Convert the received bytes to a hexadecimal string
                string hexData = BitConverter.ToString(receivedBytes).Replace("-", " "); // Space separates hex values
                Console.WriteLine($"Data received (hex): {hexData}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading data: {ex.Message}");
            }
        }

        public static int[] GetControllers(byte[] bytes)
        {
            int numberOfControllers = bytes.Length / 4;
            int[] controllers = new int[numberOfControllers];
            for(int i=0; i<bytes.Length; i+=4)
            {
                if(bytes[i] == 5)
                {
                    controllers[i / 4] = bytes[i + 1] - 33 - 23;
                }
            }
            Console.WriteLine($"Controllers found:{controllers}");
            
            return controllers;
        }
        public static void ConnectionRequest(SerialPort serialPort)
        {
            try
            {
                byte[] b = HexStringToByteArray("040A"); //Connection request
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to light all the lasers: {ex.Message}");
            }
        }

        public static void StartScanning(SerialPort serialPort)
        {
            try
            {
                byte[] b = HexStringToByteArray("080A"); //start scanning
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to light all the lasers: {ex.Message}");
            }
        }

        public static void TurnOnAllTheLasers(SerialPort serialPort)
        {
            try
            {
                byte[] b = HexStringToByteArray("070A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to light all the lasers: {ex.Message}");
            }
        }

        public static void TurnOffAllTheLasers(SerialPort serialPort)
        {
            try
            {
                byte[] b = HexStringToByteArray("060A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
        }

        public static void TurnOnTheLaserWithoutScanning(SerialPort serialPort, int laserNumber)
        {
            try
            {
                byte[] b = HexStringToByteArray($"00{laserNumber + 33:X2}0A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
        }

        public static void TurnOnTheLaserWithScanning(SerialPort serialPort, int laserNumber)
        {
            try
            {
                byte[] b = HexStringToByteArray($"01{laserNumber + 33:X2}0A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
        }

        public static void TurnOnTheLaserWithScanningRange(SerialPort serialPort, int startLaserNumber, int endLaserNumber)
        {
            try
            {
                byte[] b = HexStringToByteArray($"09{startLaserNumber + 33:X2}{endLaserNumber + 33:X2}0A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
        }

        public static void TurnOffTheLaser(SerialPort serialPort, int laserNumber)
        {
            try
            {
                byte[] b = HexStringToByteArray($"02{laserNumber + 33:X2}0A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
        }

        public static void CutTheLaser(SerialPort serialPort, int laserNumber)
        {
            try
            {
                byte[] b = HexStringToByteArray($"03{laserNumber + 33:X2}0A"); //to light the lasers
                serialPort.Write(b, 0, b.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to turn off all the lasers: {ex.Message}");
            }
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
