using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Smash : BaseGame
{
    private List<string> devices;
    private List<string> colors = new List<string> { ColorPalette.ggg, ColorPalette.rrr };
    private int numberOfDevices = 33;
    private Random random = new Random();
    private UdpHandler udpHandler;
    private MusicPlayer musicPlayer;
    private string logFile = "c:\\games\\logs\\smash.log";
    private int iterations = 0;
    private const int maxIterations = 5;
    private const double targetPercentage = 0.3; // 30% of devices
    private int targetCount;
    private HashSet<int> activeIndices;

    public Smash(TimeSpan duration, string logFilePath, UdpHandler udpHandlerInstance, MusicPlayer musicPlayerInstance)
        : base(duration)
    {
        udpHandler = udpHandlerInstance;
        musicPlayer = musicPlayerInstance;
        logFile = logFilePath;
        //Initialize();
    }

    protected override void OnStart()
    {
        base.OnStart();
        devices = new List<string>(new string[numberOfDevices]);
        for (int i = 0; i < numberOfDevices; i++)
        {
            devices[i] = ColorPalette.noColor;
        }

        targetCount = (int)(numberOfDevices * targetPercentage);
        activeIndices = new HashSet<int>();

        StartGame();
    }

    protected override void OnEnd()
    {
        base.OnEnd();
        if (Progress < 1.0)
        {
            HandleFailure();
        }
        else
        {
            musicPlayer.PlayWinMusic("content/win_music.mp3");
            LogData("Game ended successfully.");
        }
    }

    private void HandleFailure()
    {
        LogData("Game failed.");
        musicPlayer.PlayWinMusic("content/failure_music.mp3"); // Play failure music
        LogData("You failed, let's try again.");
        BlinkAll(3); // Blink all lights
     //   Score = 0; // Reset score for the next game
        Task.Delay(3000).ContinueWith(_ => OnStart()); // Restart the game after delay
    }

    private void StartGame()
    {
        musicPlayer.PlayBackgroundMusic("content/background_music.mp3");
        Console.WriteLine("Game starting in 3... 2... 1...");
        Thread.Sleep(3000); // Countdown

        gameTimer.Start();
        ActivateRandomLights();
    }

    private void ActivateRandomLights()
    {
        if (iterations >= maxIterations)
        {
           // End();
            return;
        }

        // Clear all lights
        for (int i = 0; i < numberOfDevices; i++)
        {
            devices[i] = ColorPalette.noColor;
        }

        // Activate a percentage of random lights as targets
        activeIndices.Clear();
        while (activeIndices.Count < targetCount)
        {
            int index = random.Next(numberOfDevices);
            if (!activeIndices.Contains(index))
            {
                devices[index] = ColorPalette.ggg; // Green indicates the target light
                activeIndices.Add(index);
            }
        }

        udpHandler.SendColorsToUdp(devices);
        udpHandler.BeginReceive(ReceiveCallback);

        iterations++;
    }

    protected void ProcessData(byte[] receivedBytes)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        Console.WriteLine($"Received data: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();

        foreach (int position in positions)
        {
            int actualPosition = position / 3;
            if (devices[actualPosition] == ColorPalette.ggg)
            {
                devices[actualPosition] = ColorPalette.rrr; // Change color to indicate hit
                Score++;
                activeIndices.Remove(actualPosition);
                LogData($"Hit detected at position {actualPosition}. Score: {Score}");
                musicPlayer.PlayScoreMusic("content/score_music.wav");
            }
        }

        // If all targets have been hit, activate new targets
        if (activeIndices.Count == 0 && iterations < maxIterations)
        {
            ActivateRandomLights();
        }
        else
        {
            udpHandler.SendColorsToUdp(devices);
            udpHandler.BeginReceive(ReceiveCallback);
        }
    }

    private void ReceiveCallback(byte[] receivedBytes)
    {
        try
        {
            ProcessData(receivedBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving UDP data: {ex.Message}");
            udpHandler.BeginReceive(ReceiveCallback);
        }
    }

    private void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        LogData( logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }

    private void BlinkAll(int repeat)
    {
        for (int i = 0; i < repeat; i++)
        {
            var deepCopiedList = devices.Select(x => ColorPalette.yellow).ToList();
            udpHandler.SendColorsToUdp(deepCopiedList);
            Thread.Sleep(500);
            deepCopiedList = devices.Select(x => ColorPalette.red).ToList();
            udpHandler.SendColorsToUdp(deepCopiedList);
            Thread.Sleep(500);
            LogData($"BlinkAll: {string.Join(",", deepCopiedList)}");
        }
    }
}
