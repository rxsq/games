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
    private UdpClient acknowledgmentListener;
    private int acknowledgmentPort = 11002; // Port for receiving acknowledgment
    private CancellationTokenSource cancellationTokenSource;

    public GameStatusPublisher(string ipAddress)
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 11001); // Sending to ScoreboardListener on port 11001
        cancellationTokenSource = new CancellationTokenSource();

        // Initialize the UDP client for sending
        udpClient = new UdpClient();

        // Initialize the UDP client for receiving acknowledgments
        acknowledgmentListener = new UdpClient(acknowledgmentPort);
        Task.Run(() => ListenForAcknowledgment(cancellationTokenSource.Token), cancellationTokenSource.Token);
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
                Console.WriteLine("UdpClient is not initialized. Reinitializing...");
                udpClient = new UdpClient(); // Reinitialize the UdpClient
            }

            string jsonMessage = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
            await udpClient.SendAsync(data, data.Length, remoteEndPoint);
            Console.WriteLine("Status published: " + jsonMessage);
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("UdpClient was disposed. Reinitializing...");
            udpClient = new UdpClient(); // Reinitialize the UdpClient
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in PublishStatus: " + ex.Message);
        }
    }

    private async Task ListenForAcknowledgment(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (acknowledgmentListener == null || acknowledgmentListener.Client == null)
                {
                    Console.WriteLine("Acknowledgment listener is not initialized. Reinitializing...");
                    acknowledgmentListener = new UdpClient(acknowledgmentPort); // Reinitialize the listener
                }

                UdpReceiveResult result = await acknowledgmentListener.ReceiveAsync().WithCancellation(cancellationToken);
                string ackMessage = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine("Acknowledgment received: " + ackMessage);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Acknowledgment listener was disposed. Reinitializing...");
                acknowledgmentListener = new UdpClient(acknowledgmentPort); // Reinitialize the listener
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ListenForAcknowledgment: " + ex.Message);
                await Task.Delay(1000, cancellationToken); // Wait before retrying
            }
        }
    }

    public void Close()
    {
        cancellationTokenSource.Cancel();
        udpClient?.Close();
        acknowledgmentListener?.Close();
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