using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;

public class LaserEscapeHandler
{
    private SerialPort serialPort;
    private StringBuilder buffer = new StringBuilder();  // Store incomplete messages
    private int numberOfLasers;
    private int numberOfControllers;
    public int rows;
    public int columns;
    private int numberOfLasersPerController;
    public char[] laserControllerA;
    private char[] laserControllerB;
    private List<int> activeDevices = new List<int>();

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

    public void BeginReceive(Action<List<int>> receiveCallback)
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

    private void ProcessReceivedData(string data, Action<List<int>> receiveCallback)
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
                List<int> cutLasers = new List<int>();
                if (completeMessage[0]=='a')
                {
                    cutLasers = GetCutLasers(completeMessage.Substring(1), 0);
                } else
                {
                    cutLasers = GetCutLasers(completeMessage.Substring(1), 1);
                }

                // Send processed data to callback
                receiveCallback(cutLasers);
            }
        }
    }
    private List<int> GetCutLasers(string message, int controller)
    {
        List<int> cutLasers = new List<int>();
        int maxIndex = numberOfLasersPerController * (controller + 1);
        int minIndex = numberOfLasersPerController * controller;

        foreach (int index in activeDevices.ToList())
        {
            int relativeIndex = index - minIndex;
            if (relativeIndex >= 0 && relativeIndex < message.Length && message[relativeIndex] == '1')
            {
                cutLasers.Add(index);
                SetLaserState(index, false);
            }
        }
        return cutLasers;
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
        if (laserIndex < 0 || laserIndex >= numberOfLasers) return;

        if (state && !activeDevices.Contains(laserIndex))
            activeDevices.Add(laserIndex);
        else if (!state && activeDevices.Contains(laserIndex))
            activeDevices.Remove(laserIndex);

        if (laserIndex < numberOfLasersPerController)
            laserControllerA[laserIndex] = state ? '1' : '0';
        else
            laserControllerB[laserIndex - numberOfLasersPerController] = state ? '1' : '0';
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
    public void ActivateLevel(int level)
    {
        TurnOffAllTheLasers(); // Ensure previous state is cleared

        if (level == 1)
        {
            // Turn on first row with alternating lasers
            for (int col = 0; col < columns; col += 2)
            {
                int laserIndex = (col * rows);
                SetLaserState(laserIndex, true);
            }
        }
        else if (level == 2)
        {
            // Turn on all rows except the first three
            for (int row = 3; row < rows; row++)
            {
                TurnOnRow(row);
            }
        }
        else if (level == 3)
        {
            // Turn on two rows with a 2-column gap
            for (int row = 0; row < rows; row++)
            {
                if (row % 2 == 0) // Every alternate row
                {
                    for (int col = 0; col < columns; col += 3) // 2-column gap
                    {
                        int laserIndex = (col * rows) + row;
                        SetLaserState(laserIndex, true);
                    }
                }
            }
        }
        else if (level == 4)
        {
            // Turn on all rows except the first 2
            for (int row = 2; row < rows; row++)
            {
                TurnOnRow(row);
            }
        }
        else if (level == 5)
        {
            // First 2 lasers in the first 2 columns
            for (int col = 0; col < 2; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    int laserIndex = (col * rows) + row;
                    SetLaserState(laserIndex, true);
                }
            }

            // Last 4 lasers in the next 4 columns, repeating pattern with a 3-column gap
            for (int col = 2; col < columns; col += 7) // Start from 2nd col, 3-column gap
            {
                for (int repeat = 0; repeat < 4; repeat++) // 4 columns in each repeat
                {
                    int currentCol = col + repeat;
                    if (currentCol < columns)
                    {
                        for (int row = rows - 4; row < rows; row++) // Last 4 rows
                        {
                            int laserIndex = (currentCol * rows) + row;
                            SetLaserState(laserIndex, true);
                        }
                    }
                }
            }
        }

        SendData();
        logger.Log($"Activated level {level}");
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
