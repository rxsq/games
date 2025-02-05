using System;
using System.IO.Ports;
using System.Text;
using System.Net.Http;

namespace Lib
{
    public class HandScanner:BaseScanner
    {
        private SerialPort serialPort;
        private string portName = "COM8";

        public HandScanner(string mode, string serverurl)
        {
            // Initialize the SerialPort
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None
            };

            try
            {
                serialPort.Open();
                SetBackgroundBlinkingBlue();
                process(mode, serverurl);
                SendAck();
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to open serial port: {ex.Message}");
            }
        }

        private void process(string mode, string serverurl)
        {
            httpClient = new HttpClient { BaseAddress = new Uri(serverurl) };
            serialPort.DataReceived += (sender, args) =>
            {
                SerialPort sp = (SerialPort)sender;
                string uid = "";
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
                        isScannerActive = true;
                    }
                    else
                    {
                        uid = receivedMessage.Split('\r')[0];
                        logger.Log($"Card inserted, processing... {receivedMessage}");
                        if (uid.Length == 0)
                        {
                            OnStatusChanged($"{uid}:card could not be read please try again");
                            return;
                        }
                        logger.Log($"received card number...{uid}");
                        string result = "";
                        try
                        {
                            if (!string.IsNullOrEmpty(uid))
                            {

                                if (mode == "I")
                                {
                                    result = "";
                                }
                                else if (mode == "R")
                                {
                                    result = ifCardRegisted(uid);
                                }
                                else if (mode == "V")
                                {
                                    result = ifPlayerHaveTime(uid);
                                }
                                logger.Log($"uuid:{uid} result:{result}");

                            }
                        }
                        catch (Exception ex)
                        {
                            result = ex.Message;
                            Console.WriteLine("An error occurred: " + ex.Message);
                        }
                        OnStatusChanged($"{uid}:{result}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading data: {ex.Message}");
                }
            };
        }
        public override void OnGameStatusChanged(string newStatus)
        {
            gameStatus = newStatus;
            if (gameStatus == "Running")
            {
                SetBackgroundRed(); // Set background to red if game status is "Running"
                SetNumberOfPlayersInBlue(numberOfPlayers);
            }
            else
            {
                if (numberOfPlayers == 0) SetBackgroundBlinkingBlue();
                else SetBackgroundGreen(); // Set background to green for all other statuses
                SetNumberOfPlayersInGreen(numberOfPlayers);
            }
        }
        public override void OnNumberOfPlayersChanged(int num)
        {
            numberOfPlayers = num;
            if (gameStatus == "Running") SetNumberOfPlayersInBlue(numberOfPlayers);
            else SetNumberOfPlayersInGreen(numberOfPlayers);
        }
        //private void SerialPortNFC_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    SerialPort sp = (SerialPort)sender;

        //    try
        //    {
        //        // Read the bytes from the SerialPort buffer
        //        int byteCount = sp.BytesToRead;
        //        byte[] receivedBytes = new byte[byteCount];
        //        string receivedMessage = "";
        //        sp.Read(receivedBytes, 0, byteCount);
        //        if (receivedBytes != null)
        //        {
        //            receivedMessage = Encoding.UTF8.GetString(receivedBytes);
        //        }
        //        if (receivedMessage.StartsWith("ACKCO"))
        //        {
        //            scannerActive = true;
        //        }
        //        else
        //        {
        //            string uid = receivedMessage.Split('\n')[0];

        //            if (!playersList.Contains(uid))
        //            {
        //                playersList.Add(uid);
        //                SetNumberOfPlayersInBlue(playersList.Count);
        //                if (playersList.Count == 1)
        //                {
        //                    SetBackgroundGreen();
        //                }
        //                else if (playersList.Count >= 2)
        //                {
        //                    SetBackgroundGreen();
        //                }
        //            }
        //        }
        //        Console.WriteLine($"Data received : {receivedMessage}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error reading data: {ex.Message}");
        //    }
        //}
        private void SendAck()
        {
            string message = " CONN\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetNumberOfPlayersInBlue(int num)
        {
            string message = $" L_LVLB_{num}\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetNumberOfPlayersInGreen(int num)
        {
            string message = $" L_LVLG_{num}\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetBackgroundGreen()
        {
            string message = $" L_BG_G\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetBackgroundBlue()
        {
            string message = $" L_BG_B\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetBackgroundRed()
        {
            string message = $" L_BG_R\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void SetBackgroundBlinkingBlue()
        {
            string message = $" L_BG_1\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void TurnOff()
        {
            string message = $" ABORT_1\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
        private void TurnOffWithRed()
        {
            string message = $" ABORT_R\n";
            serialPort.Write(message.ToCharArray(), 0, message.Length);
        }
    }
}
