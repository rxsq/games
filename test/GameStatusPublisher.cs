using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;

public class GameStatusPublisher
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private UdpClient listener;
    private int acknowledgmentPort = 11002; // Port for receiving acknowledgment
    string ipAddress = "169.254.255.255";
    public GameStatusPublisher()
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 4626); // Sending to ScoreboardListener on port 11001

        //listener = new UdpClient(acknowledgmentPort); // Listener for acknowledgment on port 11002
      //  Task.Run(() => ListenForAcknowledgment());
    }

    public async void PublishStatus(string msg)
    {
        byte[] data = HexStringToByteArray(msg.Replace(" ", "")
            );
             
        udpClient.Send(data, data.Length, remoteEndPoint);
    }
    public async void send(byte[] data)
    {
      
        udpClient.Send(data, data.Length, remoteEndPoint);
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
    private void ListenForAcknowledgment()
    {
        while (true)
        {
            IPEndPoint acknowledgmentEndPoint = new IPEndPoint(IPAddress.Any, acknowledgmentPort);
            byte[] receivedData = listener.Receive(ref acknowledgmentEndPoint);
            string ackMessage = Encoding.UTF8.GetString(receivedData);
            Console.WriteLine("Acknowledgment received: " + ackMessage);
           // logger.Log("Acknowledgment received: " + ackMessage);
        }
    }

    public void Close()
    {
        udpClient.Close();
        listener.Close();
    }
}
