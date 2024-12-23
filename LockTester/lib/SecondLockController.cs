using System;
using System.IO.Ports;

public class SecondLockController
{
    private SerialPort serialPort;

    public SecondLockController(string portName)
    {
        // Initialize the SerialPort
        serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None
        };

        try
        {
            serialPort.Open(); // Open the serial port
            Console.WriteLine($"Serial port {portName} opened successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an AT command to the relay controller and reads the response.
    /// </summary>
    private string SendCommand(string command)
    {
        if (!serialPort.IsOpen)
        {
            Console.WriteLine("Serial port is not open.");
            return null;
        }

        try
        {
            serialPort.Write(command.ToCharArray(), 0, command.Length); // Send the command
            string response = serialPort.ReadLine(); // Read the response
            Console.WriteLine($"Command: {command}, Response: {response}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending command: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Turns the relay ON (closes the relay).
    /// </summary>
    public void TurnRelayOn()
    {
        string response = SendCommand("AT+CH1=1");
        if (response != null && response.Contains("OK"))
        {
            Console.WriteLine("Relay turned ON.");
        }
        else
        {
            Console.WriteLine("Failed to turn relay ON.");
        }
    }

    /// <summary>
    /// Turns the relay OFF (opens the relay).
    /// </summary>
    public void TurnRelayOff()
    {
        string response = SendCommand("AT+CH1=0");
        if (response != null && response.Contains("OK"))
        {
            Console.WriteLine("Relay turned OFF.");
        }
        else
        {
            Console.WriteLine("Failed to turn relay OFF.");
        }
    }

    /// <summary>
    /// Test the communication with the relay controller.
    /// </summary>
    public string TestRelay()
    {
        string response = SendCommand("AT");
        if (response != null && response.Contains("OK"))
        {
            return "Relay communication test successful.";
        }
        else
        {
			return "Relay communication test failed.";
        }
    }

    /// <summary>
    /// Turns the relay ON and OFF in sequence.
    /// </summary>
    public void TurnOnAndOff()
    {
        for (int i = 0; i < 5; i++)
        {
            TurnRelayOn();
            System.Threading.Thread.Sleep(500); // Wait 500ms
            TurnRelayOff();
            System.Threading.Thread.Sleep(500); // Wait 500ms
        }
    }

    /// <summary>
    /// Closes the serial port when the object is disposed.
    /// </summary>
    public void Dispose()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Console.WriteLine("Serial port closed.");
        }
    }
}
