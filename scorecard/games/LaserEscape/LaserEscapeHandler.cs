using System;
using System.IO.Ports;
using System.Text;

public class LaserEscapeHandler
{
    private SerialPort serialPort;
    private StringBuilder buffer = new StringBuilder();  // Store incomplete messages
    private int numberOfLasers;
    private int numberOfControllers;
    private int rows;
    private int columns;
    private int numberOfLasersPerController;
    private char[] laserControllerA;
    private char[] laserControllerB;

    public LaserEscapeHandler(string portName, int numberOfDevices, int numberOfControllers, int rows)
    {
        this.numberOfLasers = numberOfDevices;
        this.numberOfControllers = numberOfControllers;
        this.rows = rows;
        this.columns = numberOfDevices / rows;
        numberOfLasersPerController = numberOfLasers / numberOfControllers;

        // Laser state tracking
        laserControllerA = new string('0', numberOfLasersPerController).ToCharArray(); // First 48 lasers
        laserControllerB = new string('0', numberOfLasersPerController).ToCharArray(); // Next 48 lasers
        serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None
            };

        try
        {
            serialPort.Open();
            logger.Log($"Serial port for laser escape {portName} opened successfully.");
        }
        catch (Exception ex)
        {
            logger.Log($"Failed to open serial port for laser escape handler: {ex.Message}");
        }
    }

    public void BeginReceive(Action<string> receiveCallback)
    {
        try
        {
            if (serialPort?.IsOpen != true)
            {
                logger.LogError("Serial port is not open. Cannot start receiving.");
                return;
            }

            byte[] readBuffer = new byte[1024];

            serialPort.BaseStream.BeginRead(readBuffer, 0, readBuffer.Length, ar =>
            {
                try
                {
                    int bytesRead = serialPort.BaseStream.EndRead(ar);
                    if (bytesRead > 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(readBuffer, 0, bytesRead);
                        ProcessReceivedData(receivedMessage, receiveCallback);
                    }

                    // Continue receiving
                    BeginReceive(receiveCallback);
                }
                catch (ObjectDisposedException)
                {
                    logger.LogError("SerialPort has been disposed. Stopping receive loop.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error in BeginReceive callback: {ex.Message}");
                }
            }, null);
        }
        catch (Exception ex)
        {
            logger.LogError($"Exception in BeginReceive: {ex.Message}");
        }
    }

    private void ProcessReceivedData(string data, Action<string> receiveCallback)
    {
        buffer.Append(data); // Append new data to the buffer

        while (buffer.ToString().Contains("\n"))
        {
            int newlineIndex = buffer.ToString().IndexOf("\n");

            // Extract the complete message
            string completeMessage = buffer.ToString(0, newlineIndex).Trim();
            buffer.Remove(0, newlineIndex + 1);  // Remove processed part

            if (!string.IsNullOrEmpty(completeMessage))
            {
                //ProcessMessage(completeMessage);

                // Send processed data to callback
                receiveCallback?.Invoke(completeMessage);
            }
        }
    }

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
        if(laserIndex<48)
        {
            laserControllerA[laserIndex] = state ? '1' : '0';
        }
        else if(laserIndex<96)
        {
            laserControllerB[laserIndex-48] = state ? '1' : '0';
        }
        else
        {
            logger.LogError($"Invalid laser index {laserIndex}");
        }
    }

    public void SendData()
    {
        string commandA = $"a{new string(laserControllerA)}\n";
        string commandB = $"b{new string(laserControllerB)}\n";

        SendCommand(commandA);
        SendCommand(commandB);
    }

    public void TurnOnAllTheLasers()
    {
        for(int i = 0; i < numberOfLasersPerController; i++)
        {
            laserControllerA[i] = '1';
            laserControllerB[i] = '1';
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

    private void SendCommand(string command)
    {
        try
        {
            serialPort.Write(command);
            logger.Log($"Sent command: {command.Trim()}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to send command: {ex.Message}");
        }
    }
}
