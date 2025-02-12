using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.lib
{
    public class RestartButton
    {
        public bool isButtonActive = false;
        public event EventHandler ButtonPressed;
        protected int numberOfPlayers;
        protected string gameStatus;
        private SerialPort serialPort;
        private string portName;
        private bool scanActive = false;

        public RestartButton(string port) 
        {
            portName = port;
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None
            };

            try
            {
                serialPort.Open();
                Process();
                SendAck();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //logger.LogError($"Failed to open serial port: {ex.Message}");
            }
        }
        private void OnButtonPressed()
        {
            scanActive = true;
            ButtonPressed?.Invoke(this, EventArgs.Empty);
        }
        private void Process()
        {
            serialPort.DataReceived += (sender, args) =>
            {
                SerialPort sp = (SerialPort)sender;
                try
                {
                    // Read the bytes from the SerialPort buffer
                    int byteCount = sp.BytesToRead;
                    byte[] receivedBytes = new byte[byteCount];
                    string receivedMessage = "";
                    sp.Read(receivedBytes, 0, byteCount);
                    if (receivedBytes != null)
                    {
                        receivedMessage = Encoding.UTF8.GetString(receivedBytes);
                    }
                    if (receivedMessage.StartsWith("ACKCO"))
                    {
                        isButtonActive = true;
                    }
                    else if (receivedMessage.StartsWith("BUTTON_PRESSED"))
                    {
                        OnButtonPressed();
                        StopScan();
                    }    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading data: {ex.Message}");
                }
            };
        }
        private void SendAck()
        {
            if (!serialPort.IsOpen) { logger.LogError("Serial port is not open for Restart Button"); return; }
            string message = " CONN\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void StartBlink()
        {
            if (!serialPort.IsOpen) { logger.LogError("Serial port is not open for Restart Button"); return; }
            string message = " BLINK_B\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void StopBlink()
        {
            if (!serialPort.IsOpen) { logger.LogError("Serial port is not open for Restart Button"); return; }
            string message = " BLINK_OFF\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        public void startScan()
        {
            if (!serialPort.IsOpen) { logger.LogError("Serial port is not open for Restart Button"); return; }
            if (scanActive) { return; }
            string message = " SCAN_START\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
            StartBlink();
        }
        public void StopScan()
        {
            if (!serialPort.IsOpen) { logger.LogError("Serial port is not open for Restart Button"); return; }
            string message = " SCAN_STOP\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
            StopBlink();
            scanActive = false;
        }
    }
}