using System.Net;
using System.Net.Sockets;
using System.Text;

public class ScoreboardListener
{
    private UdpClient udpClient;
    private IPEndPoint localEndPoint;
    private IPEndPoint gameEngineEndPoint;

    public ScoreboardListener()
    {
        udpClient = new UdpClient(11001); // Listening on port 11001
        localEndPoint = new IPEndPoint(IPAddress.Any, 11001);
        gameEngineEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11002); // For sending acknowledgment or other data back if needed
    }

    public void BeginReceive(Action<byte[]> receiveCallback)
    {
        try
        {
            if (udpClient == null)
            {
                return;
            }

            udpClient.BeginReceive(ar =>
            {
                byte[] receivedBytes = udpClient.EndReceive(ar, ref gameEngineEndPoint);
                receiveCallback(receivedBytes);
                logger.Log("Received data from game engine: " + Encoding.UTF8.GetString(receivedBytes));
            }, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in BeginReceive: " + ex.Message);
        }
    }

    public void SendStartGameMessage(string startMessage)
    {
        byte[] startData = Encoding.UTF8.GetBytes(startMessage);
        udpClient.Send(startData, startData.Length, gameEngineEndPoint);
        Console.WriteLine("Start game message sent.");
        logger.Log($"Start game message sent. {startMessage}");
    }

    public void Close()
    {
        udpClient.Close();
    }
}
