using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

public class Target : BaseGame
{
    private List<string> devices;
    private List<string> colors = new List<string>
    {
        ColorPalette.rgb, ColorPalette.rbg, ColorPalette.bgr, ColorPalette.brg,
        ColorPalette.grb, ColorPalette.gbr, ColorPalette.bbb, ColorPalette.ggg,
        ColorPalette.rrr, ColorPalette.nocolor3
    };
    private int numberOfDevices = 33;
    private int starIndex = 18;
    private UdpHandler udpHandler;
    private MusicPlayer musicPlayer;
    private string logFile;
    private HashSet<int> usedStarIndices = new HashSet<int>();
    private Random random = new Random();
    private int totalScore = 0;

    public Target(TimeSpan duration, string logFilePath, UdpHandler udpHandlerInstance, MusicPlayer musicPlayerInstance)
        : base(duration)
    {
        udpHandler = udpHandlerInstance;
        musicPlayer = musicPlayerInstance;
        logFile = logFilePath;

        
    }

    protected override void OnStart()
    {
        base.OnStart();
        musicPlayer.PlayBackgroundMusic("content\\background_music.mp3");
        InitializeDevices();
        InitializeColors();
        LogData($"All colors: {string.Join(",", devices)}");
        udpHandler.BeginReceive(ReceiveCallback);
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
            musicPlayer.PlayWinMusic("content\\win_music.mp3");
            LogData("Game ended successfully.");
        }
    }

    private void HandleFailure()
    {
        LogData("Game failed.");
        musicPlayer.PlayWinMusic("content\\failure_music.mp3"); // Play failure music
        LogData("You failed, let's try again.");
        BlinkAll(3); // Blink all lights
        totalScore += Score; // Accumulate score
        Score = 0; // Reset score for the next game
        Task.Delay(3000).ContinueWith(_ => OnStart()); // Restart the game after delay
    }

    private void InitializeDevices()
    {
        devices = new List<string>(new string[numberOfDevices]);
        for (int i = 0; i < numberOfDevices; i++)
        {
            devices[i] = ColorPalette.noColor;
        }
    }

    private void InitializeColors()
    {
        SetInitialColors();
    }

    private void SetInitialColors()
    {
        devices[starIndex] = ColorPalette.ggg;
        udpHandler.SendColorsToUdp(devices);

        LoopAll();
        BlinkAll(3);
        SetColorsOfDevices();
    }

    private string GetStarColor()
    {
        int index;
        do
        {
            index = random.Next(6);
        } while (usedStarIndices.Contains(index));
        usedStarIndices.Add(index);

        string starColor = colors[index];
        devices[starIndex] = starColor;
        return starColor;
    }

    private void SetColorsOfDevices()
    {
        string starColor = GetStarColor();
        int numberOfStarColorDevices = (int)Math.Round(numberOfDevices * 0.3);
        HashSet<int> usedIndices = new HashSet<int> { starIndex };

        for (int i = 0; i < numberOfStarColorDevices; i++)
        {
            int index;
            do
            {
                index = random.Next(numberOfDevices);
            } while (usedIndices.Contains(index));

            devices[index] = starColor;
            usedIndices.Add(index);
        }

        for (int i = 0; i < numberOfDevices; i++)
        {
            if (!usedIndices.Contains(i))
            {
                string newColor;
                do
                {
                    newColor = colors[random.Next(6)];
                } while (newColor == starColor);

                devices[i] = newColor;
            }
        }

        udpHandler.SendColorsToUdp(devices);
        LogData($"Sending final colors: {string.Join(",", devices)}");
        LogData($"Sending star color: {devices[starIndex]}");
    }

    protected override void OnGameTick(object sender, ElapsedEventArgs e)
    {
        base.OnGameTick(sender, e);
        Progress += 1.0 / Duration.TotalSeconds;
        if (Progress >= 1.0)
        {
            EndGame();
        }
    }

    private void ProcessData(byte[] receivedBytes)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        LogData($"Received data from {udpHandler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0 && position != starIndex * 3)
                                          .ToList();

        LogData($"Touch detected: {string.Join(",", positions)}");

        foreach (int position in positions)
        {
            int actualPos = position / 3;
            if (devices[starIndex] == devices[actualPos])
            {
                LogData("Color change detected");
                SendDataToDevice(ColorPalette.noColor, actualPos);
                musicPlayer.PlayScoreMusic("content\\score_music.wav");
                Score++;
                LogData($"Score updated: {Score}");
            }
        }

        if (devices.FindAll(x => x == devices[starIndex]).Count == 1)
        {
            Level++;
            LogData($"Level up: {Level}");
            SetColorsOfDevices();
        }

        udpHandler.BeginReceive(ReceiveCallback);
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

    private void SendDataToDevice(string color, int deviceNo)
    {
        try
        {
            devices[deviceNo] = color;
            udpHandler.SendColorsToUdp(devices);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            SendDataToDevice(color, deviceNo);
        }
    }

   

    private async Task BlinkStar()
    {
        try
        {
            var lightsToBeBlinked = devices.Select(x =>
            {
                if (x == devices[starIndex])
                {
                    return ColorPalette.yellow;
                }
                else
                {
                    return x;
                }
            }).ToList();

            udpHandler.SendColorsToUdp(lightsToBeBlinked);
            await Task.Delay(400);
            udpHandler.SendColorsToUdp(devices);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            await BlinkStar();
        }
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

    private void LoopAll()
    {
        for (int i = 0; i < 2; i++)
        {
            var deepCopiedList = devices.Select(x => ColorPalette.noColor).ToList();
            udpHandler.SendColorsToUdp(deepCopiedList);

            var loopColor = colors[random.Next(colors.Count - 1)];
            for (int j = 0; j < devices.Count; j++)
            {
                deepCopiedList[j] = loopColor;
                udpHandler.SendColorsToUdp(deepCopiedList);
                deepCopiedList[j] = ColorPalette.noColor;
                Thread.Sleep(100);
            }

            LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
        }
    }
}
