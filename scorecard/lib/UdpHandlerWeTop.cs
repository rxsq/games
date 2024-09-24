using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace scorecard.lib
{
    public class UdpHandlerWeTop: BaseUdpHandler
    {
        public UdpHandlerWeTop(string ipAddress, int destPort, int srcPort, int receiverPort, int noofledPerdevice, int columns, string namep) : base(ipAddress, destPort, srcPort, receiverPort, noofledPerdevice, columns, namep)
        {
            int noofdevices = 20;
            List<String> x = new List<String>();
            foreach (var device in Enumerable.Range(0, noofdevices))
            {
                x.Add("000000");
            }
            SendColorsToUdp(x);
            byte[] t = udpClientReceiver.Receive(ref RemoteEndPoint);
            DeviceList = this.ReceiveMessage(noofledPerdevice);
            logger.Log($"no of devices found:{DeviceList.Count} :{namep}");
            this.Rows = DeviceList.Count / columns;
            this.columns = columns;
            Task.Run(() => SendWakeup());
        }
        private void SendWakeup()
        {
            try
            {
                
                udpSender.Send(mockResponse[0], mockResponse[0].Length, destinationIpAddress, destinationPort);
                //if (sn == 1)
                //{
                //    udpSender.Send(mockResponse[1], mockResponse[1].Length, destinationIpAddress, destinationPort);
                //}
                udpSender.Send(mockResponse[2], mockResponse[2].Length, destinationIpAddress, destinationPort);
                udpSender.Send(mockResponse[3], mockResponse[3].Length, destinationIpAddress, destinationPort);
                sn++;
                Task.Delay(TimeSpan.FromSeconds(8)).Wait();
                SendWakeup();
            }
            catch (Exception ex)
            {

            }

        }
        UDPResponseFactory udp = new UDPResponseFactory();
        int sn = 1;
        byte[][] mockResponse;
        public  void SendColorsToUdp(List<string> colorList)
        {
         
            mockResponse = udp.CreateMockResponse(colorList.Count, colorList, sn);
           
            udpSender.Send(mockResponse[0], mockResponse[0].Length, destinationIpAddress, destinationPort);
            if (sn == 1)
            {
                udpSender.Send(mockResponse[1], mockResponse[1].Length, destinationIpAddress, destinationPort);                
            }
            udpSender.Send(mockResponse[2], mockResponse[2].Length, destinationIpAddress, destinationPort);
            udpSender.Send(mockResponse[3], mockResponse[3].Length, destinationIpAddress, destinationPort);
            sn++;
            // LogData($"Sent data: ffff{string.Join("", colorList)} at {destinationPort}");
        }
        public List<string> ReceiveMessage(int noofledPerdevice)
        {
            byte[] t = udpClientReceiver.Receive(ref RemoteEndPoint);
            var l = new List<string>();
            try
            {
                for (int i = 3; i < t.Length; i++)
                {
                    if (t[i].ToString() == "186")
                        return l;
                        l.Add(ColorPaletteone.NoColor);
                    
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    // Handle the timeout scenario (e.g., log the timeout or notify the user)
                    logger.LogError("Can not get signal from devices. Please check controller power. Timeout occurred while waiting for data.");
                }
                else
                {
                    // Handle other potential socket errors
                    throw;
                }
            }
            return new List<string>();
        }
    }
   
}
