using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class UdpHandler
{
    private UdpClient udpClient;
    private UdpClient udpClient2;
    private string destinationIpAddress;
    private int destinationPort;
    private int sourcePort;
    public IPEndPoint RemoteEndPoint; // Changed from property to field
    private string logFile;
    public UdpHandler(string ipAddress, int destPort, int srcPort,string logfile)
    {
        destinationIpAddress = ipAddress;
        destinationPort = destPort;
        sourcePort = srcPort;

        udpClient = new UdpClient(sourcePort);
        udpClient2 = new UdpClient(20105);
        RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        logFile = logfile;
    }

    public void BeginReceive(Action<byte[]> receiveCallback)
    {
        udpClient2.BeginReceive(ar =>
        {
            try
            {
                byte[] receivedBytes = udpClient2.EndReceive(ar, ref RemoteEndPoint);
                receiveCallback(receivedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving UDP data: {ex.Message}");
                BeginReceive(receiveCallback);
            }
        }, null);
    }

    public void SendColorsToUdp(List<string> colorList)
    {
        byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
        udpClient.Send(data, data.Length, destinationIpAddress, destinationPort);
        Console.WriteLine($"ffff{string.Join("", colorList)}");
        LogData($"ffff{string.Join("", colorList)}");
    }

    private byte[] HexStringToByteArray(string hex)
    {
        hex = hex.Replace(" ", "");
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
    private void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        File.AppendAllText(logFile, logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }
}
