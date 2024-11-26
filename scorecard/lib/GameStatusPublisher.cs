using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;

public class GameStatusPublisher
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private UdpClient listener;
    private int acknowledgmentPort = 11002; // Port for receiving acknowledgment

    public GameStatusPublisher(string ipAddress)
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 11001); // Sending to ScoreboardListener on port 11001

        listener = new UdpClient(acknowledgmentPort); // Listener for acknowledgment on port 11002
        Task.Run(() => ListenForAcknowledgment());
    }

    public async void PublishStatus(int score, int lifeLine, int level, string status, int terationTime, string game1, int iteration)
    {
        var message = new
        {
            Score = score,
            LifeLine = lifeLine,
            Level = level,
            Status = status,
            IterationTime = terationTime,
            game = game1
        };

        string jsonMessage = JsonConvert.SerializeObject(message);
        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
        await Task.Run(() => udpClient.Send(data, data.Length, remoteEndPoint));
        logger.Log("Status published: " + jsonMessage);
    }
    public async void PublishStatus(int[] score, int lifeLine, int level, string status, int terationTime, string game1, int iteration)
    {
        var message = new
        {
            Scores = score,
            LifeLine = lifeLine,
            Level = level,
            Status = status,
            IterationTime = terationTime,
            game = game1
        };

        string jsonMessage = JsonConvert.SerializeObject(message);
        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
        await Task.Run(() => udpClient.Send(data, data.Length, remoteEndPoint));
        logger.Log("Status published: " + jsonMessage);
    }
    public async void PublishStatus(int[] score, int[] lifeLines, int level, string status, int terationTime, string game1, int iteration)
    {
        var message = new
        {
            Scores = score,
            LifeLines = lifeLines,
            Level = level,
            Status = status,
            IterationTime = terationTime,
            game = game1
        };

        string jsonMessage = JsonConvert.SerializeObject(message);
        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
        await Task.Run(() => udpClient.Send(data, data.Length, remoteEndPoint));
        logger.Log("Status published: " + jsonMessage);
    }

    private void ListenForAcknowledgment()
    {
        while (true)
        {
            IPEndPoint acknowledgmentEndPoint = new IPEndPoint(IPAddress.Any, acknowledgmentPort);
            byte[] receivedData = listener.Receive(ref acknowledgmentEndPoint);
            string ackMessage = Encoding.UTF8.GetString(receivedData);
            Console.WriteLine("Acknowledgment received: " + ackMessage);
            logger.Log("Acknowledgment received: " + ackMessage);
        }
    }

    public void Close()
    {
        udpClient.Close();
        listener.Close();
    }
}