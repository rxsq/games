using System;
using System.IO.Ports;
using System.Text;

public class LaserEscapeHandler
{
    private SerialPort serialPort;

    // Event for laser data received
    public event Action<int> LaserTriggered;
    public event Action<string> DataReceived;

    public LaserEscapeHandler(string portName)
    {
        // Initialize the SerialPort
        serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None
        };

        try
        {
            serialPort.Open();
            Console.WriteLine($"Serial port {portName} opened successfully.");
            Process(); // Start listening to the serial data
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
        }
    }

    private void Process()
    {
        serialPort.DataReceived += (sender, args) =>
        {
            SerialPort sp = (SerialPort)sender;
            try
            {
                int byteCount = sp.BytesToRead;
                byte[] receivedBytes = new byte[byteCount];
                sp.Read(receivedBytes, 0, byteCount);

                if (receivedBytes.Length > 0)
                {
                    string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
                    logger.Log ($"Received message: {receivedMessage}");
                    DataReceived?.Invoke(receivedMessage);

                    if (receivedBytes[0] == 5)
                    {
                        GetControllers(receivedBytes);
                    } else
                    {
                        int laserNumber = receivedBytes[1] - 33 - 23;
                        LaserTriggered?.Invoke(laserNumber);
                    }

                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading data: {ex.Message}");
            }
        };
    }

    public int[] GetControllers(byte[] bytes)
    {
        int numberOfControllers = bytes.Length / 4;
        int[] controllers = new int[numberOfControllers];

        for (int i = 0; i < bytes.Length; i += 4)
        {
            if (bytes[i] == 5)
            {
                controllers[i / 4] = bytes[i + 1] - 33 - 23;
            }
        }
        Console.WriteLine($"Controllers found: {string.Join(", ", controllers)}");
        return controllers;
    }

    // Sends a connection request to the device
    public void ConnectionRequest()
    {
        SendCommand("040A", "Connection request sent.");
    }

    // Starts laser scanning
    public void StartScanning()
    {
        SendCommand("080A", "Started scanning lasers.");
    }

    // Turns on all lasers
    public void TurnOnAllLasers()
    {
        SendCommand("070A", "All lasers turned on.");
    }

    // Turns off all lasers
    public void TurnOffAllLasers()
    {
        SendCommand("060A", "All lasers turned off.");
    }

    // Turns on a specific laser without scanning
    public void TurnOnLaserWithoutScanning(int laserNumber)
    {
        SendCommand($"00{laserNumber + 33:X2}0A", $"Laser {laserNumber} turned on without scanning.");
    }

    // Turns on a specific laser with scanning
    public void TurnOnLaserWithScanning(int laserNumber)
    {
        SendCommand($"01{laserNumber + 33:X2}0A", $"Laser {laserNumber} turned on with scanning.");
    }

    // Turns on a range of lasers with scanning
    public void TurnOnLaserWithScanningRange(int startLaserNumber, int endLaserNumber)
    {
        SendCommand($"09{startLaserNumber + 33:X2}{endLaserNumber + 33:X2}0A", $"Lasers {startLaserNumber} to {endLaserNumber} turned on with scanning.");
    }

    // Turns off a specific laser
    public void TurnOffLaser(int laserNumber)
    {
        SendCommand($"02{laserNumber + 33:X2}0A", $"Laser {laserNumber} turned off.");
    }

    // Simulates cutting a laser
    public void CutLaser(int laserNumber)
    {
        SendCommand($"03{laserNumber + 33:X2}0A", $"Laser {laserNumber} simulated as cut.");
    }

    // Helper method to send commands
    private void SendCommand(string hexCommand, string logMessage)
    {
        try
        {
            byte[] commandBytes = HexStringToByteArray(hexCommand);
            serialPort.Write(commandBytes, 0, commandBytes.Length);
            Console.WriteLine(logMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send command: {ex.Message}");
        }
    }

    // Converts a hex string to a byte array
    private byte[] HexStringToByteArray(string hex)
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
}
