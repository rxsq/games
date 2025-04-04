using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Services
{
    internal class UdpHandler
    {
        private UdpClient udpClientReceiver;
        private UdpClient udpClientSocket2;
        private string destinationIpAddress;
        private int destinationPort;
        private int sourcePort;
        private IPEndPoint remoteEndPoint;
        private IPEndPoint remoteEndPointSocket2;
        private System.Threading.Timer relayTimer;
        private System.Threading.Timer receiveTimer;
        private string relayMessage;
        private Boolean isReceiving = true;

        public event Action<byte[]> DataReceived;

        public UdpHandler(string ipAddress, int sendPort, int receivePort, int socket2SenderPort, string initialMessage)
        {
            destinationIpAddress = ipAddress;
            relayMessage = initialMessage;


            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), receivePort);
            udpClientReceiver = new UdpClient(remoteEndPoint);
            relayTimer = new System.Threading.Timer(TargetTimeElapsed, null, 1000, 200);
            // receiveTimer = new System.Threading.Timer(TargetTimeElapsedReceiver, null, 200, 200);
            remoteEndPointSocket2 = new IPEndPoint(IPAddress.Parse(ipAddress), socket2SenderPort);
            udpClientSocket2 = new UdpClient();
        }

        private void TargetTimeElapsed(object state)
        {
            SendAsync(relayMessage);

        }

        public void BeginReceive(Action<byte[]> receiveCallback)
        {
            try
            {

                if (isReceiving)
                {
                    udpClientReceiver.BeginReceive(ar =>
                    {

                        if (isReceiving)
                        {
                            byte[] receivedBytes = udpClientReceiver.EndReceive(ar, ref remoteEndPoint);
                            receiveCallback(receivedBytes);
                        }


                    }, null);
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error receiving data: {ex.Message}");

            }
        }
        public void StopReceiving()
        {
            isReceiving = false;
            udpClientReceiver.Close();
            relayTimer.Dispose();
        }
        public void StartReceiving()
        {
            isReceiving = true;
            //udpClientReceiver = new UdpClient(remoteEndPoint);
            relayTimer = new System.Threading.Timer(TargetTimeElapsed, null, 1000, 200);
        }

        public async Task SendAsync(string message)
        {
            try
            {
                byte[] data = HexStringToByteArray(message);
                await udpClientSocket2.SendAsync(data, data.Length, remoteEndPointSocket2);
                //  logger.Log($"Sent data to {destinationIpAddress}:{destinationPort} - {BitConverter.ToString(data)}");
            }
            catch (Exception ex)
            {
                logger.Log($"Error sending data: {ex.Message}");
            }
        }

        public void Close()
        {
            udpClientReceiver.Close();

        }

        private byte[] HexStringToByteArray(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
