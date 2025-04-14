using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

public class LaserEscapeHandler
{
    private SerialPort serialPort;
    //private StringBuilder buffer = new StringBuilder();  // Store incomplete messages
    private List<byte> dataBuffer = new List<byte>(); // Global buffer to hold received data
    public int numberOfLasers;
    private int numberOfControllers;
    public int rows;
    public int columns;
    private int numberOfLasersPerController;
    public char[] laserControllerA;
    private char[] laserControllerB;
    public List<int> activeDevices = new List<int>();
    private int packetLength;
    Action<List<int>> receiveCallback;
    private bool startReceive = false;

    //exception lasers and sensors
    //private List<int> exceptionLasers = new List<int> { 0, 12, 21, 39, 45, 28, 34, 23, 41, 13, 61, 72, 84, 57, 63, 81, 58, 82, 59, 77, 56, 74, 92, 18, 20, 62, 80, 26, 32 };
    private List<int> exceptionLasers = new List<int> { 41 };
    //private List<int> alwaysZero = new List<int> { 12, 17, 19, 26, 28, 30, 33, 38, 40, 43, 45, 57, 68,69,62,64,69,73,78,80,88 };
    //private List<int> alwaysOne = new List<int> { 5, 25, 66, 96 };
    //private List<int> notTurningOff = new List<int> { 42, 61, 68, 75 };

    public LaserEscapeHandler(string portName, int numberOfDevices, int numberOfControllers, int rows, Action<List<int>> receiveCallback)
    {
        this.numberOfLasers = numberOfDevices;
        this.numberOfControllers = numberOfControllers;
        this.rows = rows;
        this.columns = numberOfDevices / rows;
        numberOfLasersPerController = numberOfLasers / numberOfControllers;
        packetLength = ((int)Math.Ceiling(numberOfLasersPerController / 8.0)) + 2;
        this.receiveCallback = receiveCallback;

        // Laser state tracking
        laserControllerA = new string('0', numberOfLasersPerController).ToCharArray(); // First 48 lasers
        laserControllerB = new string('0', numberOfLasersPerController).ToCharArray(); // Next 48 lasers
        serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None
            };

        try
        {
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            }

            logger.Log($"Serial port for laser escape {portName} opened successfully.");
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to open serial port for laser escape handler: {ex.Message}");
        }
    }
    public void MakePattern()
    {
        TurnOffAllTheLasers();
        for (int i = 0; i < 48; i++)
        {
            laserControllerA[i] = '1';
            SendData();
        }
        for (int i = 0; i < 48; i++)
        {
            laserControllerB[i] = '1';
            SendData();
        }
        
        
    }
    //public void BeginReceive(Action<List<int>> receiveCallback)
    //{
    //    try
    //    {
    //        if (serialPort?.IsOpen != true)
    //        {
    //            logger.LogError("Serial port is not open. Cannot start receiving.");
    //            return;
    //        }

    //        byte[] readBuffer = new byte[1024];

    //        serialPort.BaseStream.BeginRead(readBuffer, 0, readBuffer.Length, ar =>
    //        {
    //            try
    //            {
    //                int bytesRead = serialPort.BaseStream.EndRead(ar);
    //                if (bytesRead > 0)
    //                {
    //                    string receivedMessage = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
    //                    ProcessReceivedData(receivedMessage, receiveCallback);
    //                }

    //                // Continue receiving
    //                //BeginReceive(receiveCallback);
    //            }
    //            catch (ObjectDisposedException)
    //            {
    //                logger.LogError("SerialPort has been disposed. Stopping receive loop.");
    //            }
    //            catch (Exception ex)
    //            {
    //                logger.LogError($"Error in BeginReceive callback: {ex.Message}");
    //            }
    //        }, null);
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError($"Exception in BeginReceive: {ex.Message}");
    //    }
    //}
    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (!startReceive)
        {
            serialPort.DiscardInBuffer();
            return;
        }

        try
        {
            int bytesToRead = serialPort.BytesToRead;
            byte[] receivedPacket = new byte[bytesToRead];
            serialPort.Read(receivedPacket, 0, bytesToRead);  // Read raw bytes
            logger.Log("Received: " + BitConverter.ToString(receivedPacket));

            if (receivedPacket.Length > 0)
            {
                ProcessReceivedData(receivedPacket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error reading from serial port: {ex.Message}");
        }
    }



    public void StartReceive()
    {
        startReceive = true;
        SendCommand(new byte[] { 0xCC, 0xFF, 0x0A }); // Send a command to start receiving data
    }
    public void StopReceive()
    {
        startReceive = false;
        SendCommand(new byte[] { 0xCC, 0x00, 0x0A }); // Send a command to stop receiving data
    }
    //public void BeginReceive(Action<List<int>> receiveCallback)
    //{
    //    try
    //    {
    //        if (serialPort?.IsOpen != true)
    //        {
    //            logger.LogError("Serial port is not open. Cannot start receiving.");
    //            return;
    //        }

            

    //        // BeginRead a larger buffer to dynamically read data as it becomes available
    //        serialPort.BaseStream.BeginRead(new byte[1024], 0, 1024, ar =>
    //        {
    //            try
    //            {
    //                if (serialPort?.IsOpen != true)
    //                {
    //                    logger.LogError("Serial port is not open. Cannot start receiving.");
    //                    return;
    //                }

    //                int bytesRead = serialPort.BaseStream.EndRead(ar);
    //                if (bytesRead > 0)
    //                {
    //                    // Dynamically read and append all available data
    //                    byte[] availableData = new byte[bytesRead];
    //                    serialPort.BaseStream.Read(availableData, 0, bytesRead);
    //                    dataBuffer.AddRange(availableData);

    //                    // Log the received data (optional for debugging)
    //                    //logger.Log($"Data Received: {ConvertBytesToBinaryString(availableData)}");

    //                    // Process received data (via the callback)
    //                    ProcessReceivedData(receiveCallback);

    //                    // Continue receiving data
    //                    BeginReceive(receiveCallback); // Keep receiving new data asynchronously
    //                }
    //            }
    //            catch (ObjectDisposedException)
    //            {
    //                logger.LogError("SerialPort has been disposed. Stopping receive loop.");
    //            }
    //            catch (ThreadAbortException)
    //            {
    //                logger.LogError("Thread aborted during BeginReceive. Stopping receive loop.");
    //            }
    //            catch (Exception ex)
    //            {
    //                logger.LogError($"Error in BeginReceive callback: {ex.Message}");
    //            }
    //        }, null);
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError($"Exception in BeginReceive: {ex.Message}");
    //    }
    //}

    public string ConvertBytesToBinaryString(byte[] byteArray)
    {
        StringBuilder binaryString = new StringBuilder();

        foreach (var b in byteArray)
        {
            // Convert each byte to an 8-bit binary string
            string binary = Convert.ToString(b, 2).PadLeft(8, '0');
            binaryString.Append(binary); // Append the binary string to the result
        }

        return binaryString.ToString();
    }


    //private void ProcessReceivedData(string data, Action<List<int>> receiveCallback)
    //{
    //    buffer.Append(data); // Append new data to the buffer

    //    while (buffer.ToString().Contains("\n"))
    //    {
    //        int newlineIndex = buffer.ToString().IndexOf("\n");

    //        // Extract the complete message
    //        string completeMessage = buffer.ToString(0, newlineIndex).Trim();
    //        buffer.Remove(0, newlineIndex + 1);  // Remove processed part

    //        if (!string.IsNullOrEmpty(completeMessage))
    //        {
    //            //ProcessMessage(completeMessage);
    //            List<int> cutLasers = new List<int>();
    //            if (completeMessage[0]==0xCA)
    //            {
    //                cutLasers = GetCutLasers(completeMessage.Substring(1), 0);
    //            } else if(completeMessage[0] == 0xCA)
    //            {
    //                cutLasers = GetCutLasers(completeMessage.Substring(1), 1);
    //            }

    //            // Send processed data to callback
    //            receiveCallback(cutLasers);
    //        }
    //    }
    //}

    private void ProcessReceivedData(byte[] receivedData)
    {
        //lock (dataBuffer)
        //{
        //    while (dataBuffer.Count > 0)
        //    {
        //        // Look for the footer byte (0x0A) which signifies the end of a message
        //        int endIndex = dataBuffer.IndexOf(0x0A);

        //        if (endIndex == -1)
        //        {
        //            // If no footer found, exit the loop (waiting for more data)
        //            receiveCallback(cutLasers);
        //            break;
        //        }

        //        // Extract the complete message including the footer byte
        //        byte[] completeMessage = dataBuffer.GetRange(0, endIndex + 1).ToArray();

        //        // Process the complete message
        //        if (completeMessage.Length > 1 && completeMessage[0] == 0xCA)
        //        {
        //            List<int> cutLasers = GetCutLasers(completeMessage, 0);

        //            receiveCallback(cutLasers);
        //        }
        //        else if (completeMessage.Length > 1 && completeMessage[0] == 0xCB)
        //        {
        //            List<int> cutLasers = GetCutLasers(completeMessage, 1);
        //            receiveCallback(cutLasers);
        //        }

        //        // Remove the processed message from the buffer (including the footer byte)
        //        dataBuffer.RemoveRange(0, endIndex + 1);
        //    }
        //}
        int messageLength = ((int)Math.Ceiling(numberOfLasersPerController / 8.0))+2;
        if (receivedData.Length > 1 && receivedData[receivedData.Length-messageLength] == 0xCA)
        {
            List<int> cutLasers = GetCutLasers(receivedData, 0);
            if(cutLasers.Count>0)
                receiveCallback(cutLasers);
        }
        else if (receivedData.Length > 1 && receivedData[receivedData.Length-messageLength] == 0xCB)
        {
            List<int> cutLasers = GetCutLasers(receivedData, 1);
            if (cutLasers.Count > 0)
                receiveCallback(cutLasers);
        }
    }

    private List<int> GetCutLasers(byte[] message, int controller)
    {
        bool sendToCon = false;
        List<int> cutLasers = new List<int>();

        int maxIndex = numberOfLasersPerController * (controller + 1);
        int minIndex = numberOfLasersPerController * controller;

        int messageLength = ((int)Math.Ceiling(numberOfLasersPerController / 8.0)); 

        // Assuming the first byte is the controller identifier (0xCA or 0xCB)
        byte controllerHeader = message[0];
        byte[] sensorBytes = new byte[messageLength];

        // Extract the 6 sensor bytes from the message (excluding the controller header and footer)
        Array.Copy(message, message.Length-messageLength-1, sensorBytes, 0, 6);

        List<int> activeDevicesCopy = new List<int>(activeDevices);

        // Iterate through the list of active devices (active laser numbers)
        foreach (int laserIndex in activeDevicesCopy)
        {
            if (exceptionLasers.Contains(laserIndex)) continue;
            int relativeLaserIndex = laserIndex - minIndex;
            if(relativeLaserIndex>=0 && relativeLaserIndex<48)
            {
                // Calculate which byte and bit correspond to the current laserIndex
                int byteIndex = relativeLaserIndex / 8; // Determine the byte that holds this laser's bit
                int bitIndex = relativeLaserIndex % 8;  // Determine the bit position within that byte

                // Ensure the byteIndex is within bounds (i.e., doesn't exceed 6 bytes)
                if (byteIndex < sensorBytes.Length)
                {
                    byte sensorByte = sensorBytes[byteIndex];

                    // Check if the laser is "cut" (bit is 0, indicating LOW state)
                    if ((sensorByte & (1 << bitIndex)) == 0)
                    {
                        sendToCon = true;
                        cutLasers.Add(laserIndex); // Add to the list of cut lasers
                        SetLaserState(laserIndex, false); // Update the laser state to 'false' (cut)
                    }
                }
            }
        }
        if (sendToCon) 
            SendData();
        return cutLasers;
    }


    //private List<int> GetCutLasers(string message, int controller)
    //{
    //    List<int> cutLasers = new List<int>();
    //    int maxIndex = numberOfLasersPerController * (controller + 1);
    //    int minIndex = numberOfLasersPerController * controller;

    //    foreach (int index in activeDevices.ToList())
    //    {
    //        int relativeIndex = index - minIndex;
    //        if (relativeIndex >= 0 && relativeIndex < message.Length && message[relativeIndex] == '1')
    //        {
    //            cutLasers.Add(index);
    //            SetLaserState(index, false);
    //        }
    //    }
    //    return cutLasers;
    //}


    //private void ProcessMessage(string message)
    //{
    //    if (string.IsNullOrEmpty(message) || message.Length < 2) return;

    //    char controllerId = message[0];
    //    string sensorData = message.Substring(1);

    //    // Handle sensor trigger
    //    for (int i = 0; i < sensorData.Length; i++)
    //    {
    //        if (sensorData[i] == '1')
    //        {
    //            int sensorNumber = (controllerId == 'a') ? i : i + 48;
    //        }
    //    }
    //}

    public void SetLaserState(int laserIndex, bool state)
    {
        if (laserIndex < 0 || laserIndex >= numberOfLasers) return;

        if (state && !activeDevices.Contains(laserIndex))
            activeDevices.Add(laserIndex);
        //else if (!state && activeDevices.Contains(laserIndex))
        //    activeDevices.Remove(laserIndex);

        if (laserIndex < numberOfLasersPerController)
            laserControllerA[laserIndex] = state ? '1' : '0';
        else
            laserControllerB[laserIndex - numberOfLasersPerController] = state ? '1' : '0';
    }


    public void SendData()
    {
        byte[] commandA = BuildLaserCommand(laserControllerA, 0xFA);
        byte[] commandB = BuildLaserCommand(laserControllerB, 0xFB);

        SendCommand(commandA);
        SendCommand(commandB);
    }

    public void TurnOnAllTheLasers()
    {
        for(int i = 0; i < numberOfLasersPerController; i++)
        {
            laserControllerA[i] = '1';
            laserControllerB[i] = '1';
            activeDevices.Add(i);
            activeDevices.Add(i + numberOfLasersPerController);
        }
        SendData();
        logger.Log("Turned On all the lasers");
    }
    public void TurnOffAllTheLasers()
    {
        for (int i = 0; i < numberOfLasersPerController; i++)
        {
            laserControllerA[i] = '0';
            laserControllerB[i] = '0';
        }
        activeDevices.Clear();
        SendData();
        logger.Log("Turned Off all the lasers");
    }
    public void TurnOnRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= rows)
        {
            logger.LogError($"Invalid row index {rowIndex}");
            return;
        }

        for (int col = 0; col < columns; col++)
        {
            int laserIndex = (col * rows) + rowIndex; // Calculate laser index for each column in the given row
            SetLaserState(laserIndex, true);
        }
        SendData();
        logger.Log($"Turned on row {rowIndex}");
    }

    public void TurnOnColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= columns)
        {
            logger.LogError($"Invalid column index {columnIndex}");
            return;
        }

        for (int row = 0; row < rows; row++)
        {
            int laserIndex = (columnIndex * rows) + row; // Calculate laser index for each row in the given column
            SetLaserState(laserIndex, true);
        }
        SendData();
        logger.Log($"Turned on column {columnIndex}");
    }

    private void SendCommand(byte[] command)
    {
        try
        {
            serialPort.Write(command, 0, command.Length);
            Thread.Sleep(100); 
            logger.Log($"Sent command: {command}x2");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to send command: {ex.Message}");
        }
    }

    private byte[] BuildLaserCommand(char[] laserArray, byte controllerId)
    {
        int byteCount = (int)Math.Ceiling(laserArray.Length / 8.0);
        byte[] command = new byte[byteCount + 2];

        command[0] = controllerId; // Start byte

        for (int i = 0; i < byteCount; i++)
        {
            int startIdx = i * 8;
            int endIdx = Math.Min(startIdx + 8, laserArray.Length);
            byte laserByte = 0;

            for (int j = startIdx; j < endIdx; j++)
            {
                if (laserArray[j] == '1')
                {
                    laserByte |= (byte)(1 << (j % 8));
                }
            }

            command[i + 1] = laserByte;
        }

        command[command.Length - 1] = 0x0A; // End byte
        return command;
    }


    public void Dispose()
    {
        if (serialPort?.IsOpen == true)
        {
            serialPort.Close();
        }
        serialPort?.Dispose();
    }
}
