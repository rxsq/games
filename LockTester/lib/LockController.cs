using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

public class LockController
{
    private SerialPort serialPort;

    public LockController(string portName)
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

    public void TurnRelayOn()
    {
        if (serialPort.IsOpen)
        {
            serialPort.WriteLine("ON"); // Send the "ON" command
            Console.WriteLine("Relay turned ON.");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    public void TurnRelayOff()
    {
        if (serialPort.IsOpen)
        {
            serialPort.WriteLine("OFF"); // Send the "OFF" command
            Console.WriteLine("Relay turned OFF.");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    public void Dispose()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Console.WriteLine("Serial port closed.");
        }
    }


}
