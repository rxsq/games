using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class GameStatusPublisher
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private int gameSelectionPort = 11002; // Port for receiving acknowledgment
    private CancellationTokenSource cancellationTokenSource;
    public string[] playerList;
    public string waitingStaus = "false";

    private static readonly Lazy<GameStatusPublisher> _instance =
        new Lazy<GameStatusPublisher>(() => new GameStatusPublisher("127.0.0.1")); // Default IP (modify if needed)

    public GameStatusPublisher(string ipAddress)
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 11001); // Sending to ScoreboardListener on port 11001
        cancellationTokenSource = new CancellationTokenSource();

        // Initialize the UDP client for sending
        udpClient = new UdpClient(gameSelectionPort);
    }

    // Public method to access the single instance
    public static GameStatusPublisher Instance => _instance.Value;

    public void BeginReceive(Action<string> receiveCallback)
    {
        try
        {
            Task.Run(() => ReceiveMessageFromGameSelection(receiveCallback, cancellationTokenSource.Token), cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in BeginReceive: " + ex.Message);
        }
    }

    public async void PublishStatus(int score, int lifeLine, int level, string status, int iterationTime, string game1, int iteration)
    {
        await PublishStatusInternal(new
        {
            Score = score,
            LifeLine = lifeLine,
            Level = level,
            Status = status,
            IterationTime = iterationTime,
            game = game1
        });
    }

    public async void PublishStatus(int[] score, int lifeLine, int level, string status, int iterationTime, string game1, int iteration)
    {
        await PublishStatusInternal(new
        {
            Scores = score,
            LifeLine = lifeLine,
            Level = level,
            Status = status,
            IterationTime = iterationTime,
            game = game1
        });
    }

    public async void PublishStatus(int[] score, int[] lifeLines, int level, string status, int iterationTime, string game1, int iteration)
    {
        await PublishStatusInternal(new
        {
            Scores = score,
            LifeLines = lifeLines,
            Level = level,
            Status = status,
            IterationTime = iterationTime,
            game = game1
        });
    }

    private async Task PublishStatusInternal(object message)
    {
        try
        {
            if (udpClient == null || udpClient.Client == null)
            {
                logger.Log("UdpClient is not initialized. Reinitializing...");
                udpClient = new UdpClient(); // Reinitialize the UdpClient
            }

            string jsonMessage = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            await udpClient.SendAsync(data, data.Length, remoteEndPoint);
            logger.Log("Status published: " + jsonMessage);
        }
        catch (ObjectDisposedException)
        {
            logger.Log("UdpClient was disposed. Reinitializing...");
            udpClient = new UdpClient(); // Reinitialize the UdpClient
        }
        catch (Exception ex)
        {
            logger.Log("Error in PublishStatus: " + ex.Message);
        }
    }

    private async Task ReceiveMessageFromGameSelection(Action<string> receiveCallback, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (udpClient == null || udpClient.Client == null)
                {
                    logger.Log("Game Selection listener is not initialized. Reinitializing...");
                    udpClient = new UdpClient(gameSelectionPort); // Reinitialize the listener
                }

                UdpReceiveResult result = await udpClient.ReceiveAsync().WithCancellation(cancellationToken);
                string message = Encoding.UTF8.GetString(result.Buffer);
                logger.Log("Message received from Game Selection: " + message);
                receiveCallback(message);

                
            }
            catch (ObjectDisposedException)
            {
                logger.Log("Game Selection listener was disposed. Reinitializing...");
                udpClient = new UdpClient(gameSelectionPort); // Reinitialize the listener
            }
            catch (System.Net.Sockets.SocketException)
            {
                logger.Log("Game Selection listener was disposed. Reinitializing...");
                udpClient = new UdpClient(gameSelectionPort); // Reinitialize the listener
            }
            catch (Exception ex)
            {
                logger.LogError("Error in Game Selection Listner: " + ex.Message);
                await Task.Delay(1000, cancellationToken); // Wait before retrying
            }
        }
    }

    public void Close()
    {
        cancellationTokenSource.Cancel();
        udpClient?.Close();
        udpClient?.Close();
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        return await task;
    }
}