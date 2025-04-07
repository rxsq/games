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

public  class UdpHandler: BaseUdpHandler
{
   
    public UdpHandler(string ipAddress, int destPort, int srcPort, int receiverPort, int noofledPerdevice, int columns, string namep):base(ipAddress, destPort, srcPort, receiverPort, noofledPerdevice, columns, namep)
    {


        DeviceList = ReceiveMessage(noofledPerdevice);
        logger.Log($"no of devices found:{DeviceList.Count} :{namep}");
        this.Rows = DeviceList.Count / columns;
        this.columns = columns;


    }
     public override async void SendColorsToUdp(List<string> colorList)
        {

            byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
            try
            {
                if (udpSender != null)
                {
                    udpSender.Send(data, data.Length, destinationIpAddress, destinationPort);
                }
            }
            catch (Exception ex)
            {
                LogData(ex.StackTrace);
            }
            // LogData($"Sent data: ffff{string.Join("", colorList)} at {destinationPort}");
        }
        public override async Task SendColorsToUdpAsync(List<string> colorList)
        {
            byte[] data = HexStringToByteArray($"ffff{string.Join("", colorList.ToArray())}");
            try
            {
                if (udpSender != null)
                {
                    await udpSender.SendAsync(data, data.Length, destinationIpAddress, destinationPort);
                }
                //Console.WriteLine($"Sent data to {destinationIpAddress}:{destinationPort} - {BitConverter.ToString(data)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data: {ex.Message}");
            }
        }

   
}
