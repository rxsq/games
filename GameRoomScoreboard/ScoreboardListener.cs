using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ScoreboardListener
{
    private UdpClient udpClient;
    private IPEndPoint localEndPoint;
    private IPEndPoint gameEngineEndPoint;
    private CancellationTokenSource cancellationTokenSource;

    public ScoreboardListener()
    {
        localEndPoint = new IPEndPoint(IPAddress.Any, 11001);
        gameEngineEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11002);
        cancellationTokenSource = new CancellationTokenSource();
    }

    public void BeginReceive(Action<byte[]> receiveCallback)
    {
        try
        {
            udpClient = new UdpClient(localEndPoint);

            Task.Run(() => ReceiveLoop(receiveCallback, cancellationTokenSource.Token), cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in BeginReceive: " + ex.Message);
        }
    }

    private async Task ReceiveLoop(Action<byte[]> receiveCallback, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (udpClient == null || udpClient.Client == null)
                {
                    Console.WriteLine("UdpClient is not initialized.");
                    await Task.Delay(1000, cancellationToken); // Wait before retrying
                    continue;
                }

                UdpReceiveResult result = await udpClient.ReceiveAsync().WithCancellation(cancellationToken);
                byte[] receivedBytes = result.Buffer;
                receiveCallback(receivedBytes);
                Console.WriteLine("Received data from game engine: " + Encoding.UTF8.GetString(receivedBytes));
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("UdpClient was disposed. Reinitializing...");
                udpClient = new UdpClient(localEndPoint); // Reinitialize the UdpClient
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ReceiveLoop: " + ex.Message);
                await Task.Delay(1000, cancellationToken); // Wait before retrying
            }
        }
    }

    public void SendStartGameMessage(string startMessage)
    {
        try
        {
            if (udpClient == null || udpClient.Client == null)
            {
                Console.WriteLine("UdpClient is not initialized.");
                return;
            }

            byte[] startData = Encoding.UTF8.GetBytes(startMessage);
            udpClient.Send(startData, startData.Length, gameEngineEndPoint);
            Console.WriteLine("Start game message sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in SendStartGameMessage: " + ex.Message);
        }
    }

    public void Close()
    {
        cancellationTokenSource.Cancel();
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