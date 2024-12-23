using System;
using System.Collections.Generic;
using System.IO.Ports;

class ComPortHandlerLaser
{
    private int start_laser = 32;
    private int laserPerController = 24;
    private int numbersOfControllers;
    private int waitTime = 100;
    private Dictionary<int, bool> controllersActive = new Dictionary<int, bool>();
    private SerialPort serialPort;

    private System.Timers.Timer timer;

    public ComPortHandlerLaser(int start_laser, int laserPerController, int baudRate, string portName, int waitTime)
    {
        this.start_laser = start_laser;
        this.laserPerController = laserPerController;
        this.waitTime = waitTime;
        this.serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
        }; 
        Initialize();
    }

    private void Initialize()
    {
        serialPort.DataReceived += SerialPort_DataReceived;
        serialPort.Open();
        for(int i = 0; i< numbersOfControllers; i++)
        {
            controllersActive[i] = false;
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;

        try
        {
            int byteCount = sp.BytesToRead;
            numbersOfControllers = byteCount / 4;
            byte[] receivedBytes = new byte[byteCount];
            if (receivedBytes != null && receivedBytes[0] == 5)
            {
                GetControllers(receivedBytes);
            }
            sp.Read(receivedBytes, 0, byteCount);
            int laserno = receivedBytes[1] - start_laser - laserPerController;

            string hexData = BitConverter.ToString(receivedBytes).Replace("-", " ");
            Console.WriteLine($"Data received (hex): {hexData}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading data: {ex.Message}");
        }
    }
    public void GetControllers(byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i += 4)
        {
            
            if (bytes[i] == 5)
            {
                int controllerNo = ((int)bytes[i] - 33 - 23)/24;
                if(controllersActive.ContainsKey(controllerNo))
                {
                    controllersActive[controllerNo] = true;
                    logger.Log($"controller {controllerNo} is active");
                }
                
            }
        }
    }

    public void ConnectionRequest(SerialPort serialPort)
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

    public void StartScanning(SerialPort serialPort)
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

    public void TurnOnAllTheLasers(SerialPort serialPort)
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

    public void TurnOffAllTheLasers(SerialPort serialPort)
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

    public void TurnOnTheLaserWithoutScanning(SerialPort serialPort, int laserNumber)
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

    public void TurnOnTheLaserWithScanning(SerialPort serialPort, int laserNumber)
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

    public void TurnOnTheLaserWithScanningRange(SerialPort serialPort, int startLaserNumber, int endLaserNumber)
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

    public void TurnOffTheLaser(SerialPort serialPort, int laserNumber)
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

    public void CutTheLaser(SerialPort serialPort, int laserNumber)
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

