using scorecard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class LightChaserGame
{
    private List<string> devices;
    private List<string> colors = new List<string> { ColorPalette.ggg, ColorPalette.rrr };
    private int numberOfDevices = 33;
    private int score = 0;
    private int currentLightIndex = -1;
    private Timer gameTimer;
    private Timer moveTimer;
    private Random random = new Random();
    private UdpHandler udpHandler;
    private MusicPlayer musicPlayer;
    private string logFile = "c:\\games\\logs\\lightshaser.log";

    public LightChaserGame()
    {
        udpHandler = new UdpHandler("192.168.0.7", 21, 7113, logFile);
        musicPlayer = new MusicPlayer();
        Initialize();
    }

    private void Initialize()
    {
        devices = new List<string>(new string[numberOfDevices]);
        for (int i = 0; i < numberOfDevices; i++)
            devices[i] = ColorPalette.noColor;

        StartGame();
    }

    private void StartGame()
    {
        musicPlayer.PlayBackgroundMusic("content/background_music.mp3");
        Console.WriteLine("Game starting in 3... 2... 1...");
        Thread.Sleep(3000); // Countdown

        gameTimer = new Timer(GameTimeElapsed, null, 60000, Timeout.Infinite); // 60-second game timer
        moveTimer = new Timer(MoveLight, null, 0, 2000); // Move light every second

        ActivateRandomLight();
    }

    private void ActivateRandomLight()
    {
        // Clear the previous light
        if (currentLightIndex >= 0 && currentLightIndex < numberOfDevices)
        {
            devices[currentLightIndex] = ColorPalette.noColor;
        }

        // Activate a new random light
        currentLightIndex = random.Next(numberOfDevices);
        devices[currentLightIndex] = ColorPalette.ggg; // Green indicates the target light

        udpHandler.SendColorsToUdp(devices);
        udpHandler.BeginReceive(ReceiveCallback);
    }

    private void ReceiveCallback(byte[] receivedBytes)
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
            int actualpos = (position / 3);
            if (actualpos == currentLightIndex)
            {
                devices[actualpos] = ColorPalette.rrr; // Change color to indicate hit
                score++;
                LogData($"Hit detected at position {actualpos}. Score: {score}");
                musicPlayer.PlayScoreMusic("content/score_music.wav");

                // Activate a new random light
                ActivateRandomLight();
                break; // Handle one hit at a time
            }
        }

        udpHandler.SendColorsToUdp(devices);
        udpHandler.BeginReceive(ReceiveCallback);
    }

    private void MoveLight(object state)
    {
        // Move to a new random light if not hit
        ActivateRandomLight();
    }

    private void GameTimeElapsed(object state)
    {
        Console.WriteLine("Time's up!");
        LogData($"Final Score: {score}");
        musicPlayer.PlayWinMusic("content/win_music.mp3");

        // Optionally, restart the game or end it
        // Initialize();
    }

    private void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        File.AppendAllText(logFile, message);
        Console.WriteLine(logMessage);
    }
}
