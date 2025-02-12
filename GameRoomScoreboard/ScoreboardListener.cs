using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        udpClient = new UdpClient(localEndPoint);
    }

    public void BeginReceive(Action<byte[]> receiveCallback)
    {
        try
        {
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
            SendMessageToGameEngine(startMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in SendStartGameMessage: " + ex.Message);
        }
    }
    public void SendPlayersListToGameEngine(string[] uids)
    {
        try
        {
            string combinedMessage = string.Join(":", uids);
            SendMessageToGameEngine("players:" + combinedMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in sending players list to game engine: " + ex.Message);
        }
    }
    public void SendWaitingPlayersStatus(string message)
    {
        try
        {
            SendMessageToGameEngine("waitingStatus:" + message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in waiting status to game engine: " + ex.Message);
        }
    }
    public void SendMessageToGameEngine(string message)
    {
        try
        {
            if (udpClient == null || udpClient.Client == null)
            {
                Console.WriteLine("UdpClient is not initialized.");
                return;
            }

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            // Send the message to the game engine
            udpClient.Send(messageData, messageData.Length, gameEngineEndPoint);
            Console.WriteLine("Message sent to game engine: " + message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in SendMessageToGameEngine: " + ex.Message);
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