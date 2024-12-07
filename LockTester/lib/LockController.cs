using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
//using (SerialPort port = new SerialPort("COM3", 9600, Parity.None, 8, ))

public class LockController
{
    private SerialPort serialPort;

    byte[] cmdON = { 0xA0, 0x01, 0x01, 0xA2 };
    byte[] cmdOFF = { 0xA0, 0x01, 0x00, 0xA1 };

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
            serialPort.Write(cmdON, 0, cmdON.Length); // Send the "ON" command
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
            serialPort.Write(cmdOFF, 0, cmdOFF.Length); // Send the "OFF" command
            Console.WriteLine("Relay turned OFF.");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    public void TurnonAndOff()
    {
        for(int i=0; i<=5;i++)
        {
            TurnRelayOn();
            TurnRelayOff();
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
