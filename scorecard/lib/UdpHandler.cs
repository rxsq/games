using Microsoft.SqlServer.Server;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class UdpHandler
{
    private UdpClient udpClient;
    private UdpClient udpClient2;
    private string destinationIpAddress;
    private int destinationPort;
    private int sourcePort;
    public IPEndPoint RemoteEndPoint;
    
   // private Logger logger;
    private bool receiving;
    public int columns;
    public int Rows;
    public string name;
    public List<int> activeDevices = new List<int>();
    public Dictionary<int, List<int>> activeDevicesGroup = new Dictionary<int, List<int>>();
    public List<string> DeviceList { get; private set; }

    public UdpHandler(string ipAddress, int destPort, int srcPort, int receiverPort, int noofledPerdevice, int columns, string namep)
    {
       // logger = new AsyncLogger(namep);
        destinationIpAddress = ipAddress;
        destinationPort = destPort;
        sourcePort = srcPort;
      

        this.name = namep;
        udpClient2 = new UdpClient(receiverPort);
        udpClient = new UdpClient(sourcePort);
        RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        
        DeviceList = ReceiveMessage(noofledPerdevice);

        this.Rows = DeviceList.Count / columns;
        this.columns = columns;
        // this .Rows = rows;
        receiving = false;
    }
    public List<string> ReceiveMessage(int noofledPerdevice)
    {
        byte[] t = udpClient2.Receive(ref RemoteEndPoint);
        int o;
        int noofdevices = Math.DivRem((t.Length - 2), noofledPerdevice, out o);

        var l = new List<string>(noofdevices);
        for (int i = 0; i < noofdevices; i++)
        {
            l.Add(noofledPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3);
            //  deviceMap.Add(i, new Device { color = noofledPerdevice==1?ColorPaletteone.NoColor: ColorPalette.noColor3, isActive = false, sequence = i });
        }
        return l;
    }
    public void BeginReceive(Action<byte[]> receiveCallback)
    {
        try
        {
            if (udpClient2 == null)
            {
                return;
            }
            receiving = true;
            udpClient2.BeginReceive(ar =>
            {
                if (receiving)
                {
                    byte[] receivedBytes = udpClient2.EndReceive(ar, ref RemoteEndPoint);
                    receiveCallback(receivedBytes);
                }

            }, null);
        }
        catch (Exception ex)
        {
            LogData(ex.StackTrace);

        }
    }

    public void StopReceive()
    {
        receiving = false;
    }

    public async void SendColorsToUdp(List<string> colorList)
    {

        byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
        try
        {
            udpClient.Send(data, data.Length, destinationIpAddress, destinationPort);
                }
        catch(Exception ex){
            LogData(ex.StackTrace);
        }
       // LogData($"Sent data: ffff{string.Join("", colorList)} at {destinationPort}");
    }
    public async Task SendColorsToUdpAsync(List<string> colorList)
    {
        byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
        try
        {
            await udpClient.SendAsync(data, data.Length, destinationIpAddress, destinationPort);
            //Console.WriteLine($"Sent data to {destinationIpAddress}:{destinationPort} - {BitConverter.ToString(data)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending data: {ex.Message}");
        }
    }
    public async Task SendColorsToUdpAsyncOne(List<string> colorList)
    {
        byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
        try
        {
            udpClient.Send(data, data.Length, destinationIpAddress, destinationPort);
            //Console.WriteLine($"Sent data to {destinationIpAddress}:{destinationPort} - {BitConverter.ToString(data)}");
        }
        catch (Exception ex)
        {
            LogData($"Error sending data: {ex.Message}");
        }
    }

    private byte[] HexStringToByteArray(string hex)
    {
      //  hex = hex.Replace(" ", "");
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    private void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fef} {message}";
        logger.Log(logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }

    public void Close()
    {
        StopReceive();
        //logger.Dispose();
        udpClient.Close();
        udpClient2.Close();
    }
}
