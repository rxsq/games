# Door Lock Control with SRD-05VDC-SL-C Relay and DC Power Supply

This document explains the setup and connections between a **NO door lock**, a **Songle SRD-05VDC-SL-C relay**, and a **DC power supply**, along with the relay's interaction with the PC port.

## Components Involved
- **NO Door Lock**: A normally open (NO) type door lock that can be powered on/off using the relay.
- **Songle SRD-05VDC-SL-C Relay**: A 5V DC relay used to control the flow of current to the door lock.
- **DC Power Supply**: Provides the necessary voltage for the door lock.
- **PC Port**: Interfaces with the relay to control the door lock via a relay trigger signal (typically, a GPIO or serial port).

## Connection Overview

### 1. **Door Lock Connection**
- **Positive (+ve) wire of the door lock** connects to the **+ve terminal of the power supply**.
- **Negative (-ve) wire of the door lock** connects to the **COM (Common) terminal of the relay**.

### 2. **Power Supply Connection**
- **Positive (+ve) terminal of the power supply** connects to the **+ve terminal of the door lock**.
- **Negative (-ve) terminal of the power supply** connects to the **NO (Normally Open) terminal of the relay**.

### 3. **Relay Connection**
- **COM (Common) terminal of the relay** connects to the **negative (-ve) wire of the door lock**.
- **NO (Normally Open) terminal of the relay** connects to the **negative (-ve) terminal of the power supply**.

## Wiring Diagram
![Wiring Diagram](wiring-diagram.png)

## How It Works
When the relay is triggered (via a control signal from the PC or another device):
1. **Relay Action**: The NO (Normally Open) contact of the relay closes, connecting the **negative terminal of the power supply** to the **negative terminal of the door lock**.
2. **Current Flow**: With both the positive terminal of the power supply connected to the positive terminal of the door lock, and the negative terminals now connected by the relay, current flows through the door lock, energizing it and causing it to activate (unlock).
3. **Power Supply Role**: The DC power supply provides the necessary voltage and current to the door lock, while the relay acts as a switch to control the connection between the power supply and the door lock.

## PC Control via Relay
- The relay can be controlled through a PC port (typically using a GPIO pin or a serial interface). The control signal from the PC triggers the relay to switch its contacts (COM and NO), allowing or disallowing current to flow to the door lock, thus locking or unlocking the door.

## C# Code Snippets for Relay Control

The following **C# class** demonstrates how to control the relay connected to a serial port, turning the door lock ON and OFF.

### `LockController` Class

```csharp
using System;
using System.IO.Ports;

public class LockController : IDisposable
{
    private SerialPort serialPort;

    // Commands to turn the relay ON and OFF
    private readonly byte[] cmdON = { 0xA0, 0x01, 0x01, 0xA2 };
    private readonly byte[] cmdOFF = { 0xA0, 0x01, 0x00, 0xA1 };

    // Constructor: Initialize the SerialPort
    public LockController(string portName)
    {
        serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None
        };

        try
        {
            serialPort.Open();
            Console.WriteLine($"Serial port {portName} opened successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
        }
    }

    // Method to turn the relay ON
    public void TurnRelayOn()
    {
        if (serialPort.IsOpen)
        {
            serialPort.Write(cmdON, 0, cmdON.Length);
            Console.WriteLine("Relay turned ON.");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    // Method to turn the relay OFF
    public void TurnRelayOff()
    {
        if (serialPort.IsOpen)
        {
            serialPort.Write(cmdOFF, 0, cmdOFF.Length);
            Console.WriteLine("Relay turned OFF.");
        }
        else
        {
            Console.WriteLine("Serial port is not open.");
        }
    }

    // Method to toggle the relay ON and OFF multiple times
    public void ToggleRelay(int cycles)
    {
        for (int i = 0; i < cycles; i++)
        {
            TurnRelayOn();
            System.Threading.Thread.Sleep(1000); // Wait for 1 second
            TurnRelayOff();
            System.Threading.Thread.Sleep(1000); // Wait for 1 second
        }
    }

    // Method to release resources
    public void Dispose()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Console.WriteLine("Serial port closed.");
        }
    }
}
```

## Conclusion
This setup allows you to control a normally open (NO) door lock using a relay and a DC power supply. The relay acts as an intermediary that opens and closes the circuit between the power supply and the door lock, allowing the door lock to be activated or deactivated through the control signal sent from the PC.
