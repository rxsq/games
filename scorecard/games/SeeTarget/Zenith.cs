using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

class Zenith: BaseMultiplayerGame
{
    private readonly List<int> blueTilePos;
    private int greenTilePos;
    private List<int> redTilePos;
    private readonly int totalBlueTargetsPerIteration;
    private int totalRedTargetsPerIteration;
    private Task greenLightTask;
    private readonly string blueTargetColor;
    private readonly string redTargetColor;
    private readonly string greenTargetColor;
    private readonly string targetNoColor;
    public Zenith(GameConfig config) : base(config) 
    {
        totalBlueTargetsPerIteration = config.MaxPlayers;
        totalRedTargetsPerIteration = 0;
        blueTilePos = new List<int>();
        greenTilePos = -1;
        blueTargetColor = config.NoofLedPerdevice == 1 ? ColorPaletteone.Blue : ColorPalette.Blue;
        redTargetColor = config.NoofLedPerdevice == 1 ? ColorPaletteone.Red : ColorPalette.Red;
        greenTargetColor = config.NoofLedPerdevice == 1 ? ColorPaletteone.Green : ColorPalette.Green;
        targetNoColor = config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3;
        for (int i = 0; i < config.MaxPlayers; i++)
        {
            LifeLines[i] = 5;
        }
    }
    protected override async void StartAnimition()
    {
        base.StartAnimition();

    }
    protected override void Initialize()
    {
        BlinkAllAsync(2);
    }
    protected override void OnStart()
    {
        if (greenLightTask == null || greenLightTask.IsCompleted)
        {
            if (greenLightTask != null && !greenLightTask.IsCompleted)
            {
                logger.Log("greenLightTask task still running");
            }
            logger.Log("Starting greenLightTask task");
            greenLightTask = Task.Run(() => blinkGreenLight());
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    protected override void OnIteration()
    {
        SendSameColorToAllDevice(targetNoColor);
        totalRedTargetsPerIteration = Math.Min((Level - 1) * 2, handler.DeviceList.Count - totalBlueTargetsPerIteration);
        redTilePos = new List<int>();
        SetTargets();
        BlinkAllAsync(1);
    }

    private void blinkGreenLight()
    {
        if (!isGameRunning)
            return;

        greenTilePos = random.Next(handler.DeviceList.Count);
        if(blueTilePos.Contains(greenTilePos)) blueTilePos.Remove(greenTilePos);
        else if(redTilePos.Contains(greenTilePos)) redTilePos.Remove(greenTilePos);

        ChnageColorToDevice(greenTargetColor, greenTilePos, handler);
        handler.activeDevices.Add(greenTilePos);

        int ran = random.Next(10)+10; //generate from 10s to 20s of interval

        if (isGameRunning)
        {
            Thread.Sleep(ran * 1000);
            blinkGreenLight();
        }
    }

    private void SetTargets()
    {
        handler.activeDevices.Clear();
        blueTilePos.Clear();
        redTilePos.Clear();

        for (int i = 0; i < totalBlueTargetsPerIteration; i++)
        {
            int index;
            do
            {
                index = random.Next(0, handler.DeviceList.Count());
            } while (handler.activeDevices.Contains(index) || index == greenTilePos);
            handler.DeviceList[index] = blueTargetColor;
            handler.activeDevices.Add(index);
            blueTilePos.Add(index);
        }

        if (totalRedTargetsPerIteration >= handler.DeviceList.Count - totalBlueTargetsPerIteration)
        {
            int count = 0;
            for (int i = 0; i < handler.DeviceList.Count; i++)
            {
                if (!handler.activeDevices.Contains(i) && i != greenTilePos)
                {
                    handler.activeDevices.Add(i);
                    handler.DeviceList[i] = redTargetColor;
                    redTilePos.Add(i);
                    count++;
                }
            }
        }
        else
        {
            for (int i = 0; i < totalRedTargetsPerIteration; i++)
            {
                int index;
                int count = 0;
                do
                {
                    index = random.Next(0, handler.DeviceList.Count());
                    count++;
                } while ((handler.activeDevices.Contains(index) || index == greenTilePos) && count<100);
                handler.activeDevices.Add(index);
                handler.DeviceList[index] = redTargetColor;
                redTilePos.Add(index);
            }
        }
        LogData($"Blue Tiles {string.Join(", ", blueTilePos)}, Red Tile {string.Join(", ", redTilePos)}");
        handler.SendColorsToUdp(handler.DeviceList);
    }
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        var positions = receivedData.Select((value, index) => new { value, index })
                                .Where(x => x.value >= 0x0A && x.value <= 0x0E) // Match player identifiers
                                .Select(x => new
                                {
                                    Player = x.value - 0x0A, // Calculate player number (0 for 0x0A, 1 for 0x0B, etc.)
                                    Position = (x.index - 2) / config.NoofLedPerdevice // Calculate position
                                }).ToList();
        foreach (var pos in positions)
        {
            int playerNumber = pos.Player;
            int td = pos.Position;

            if (LifeLines[playerNumber] <= 0)
            {
                LogData($"Player {playerNumber} has no remaining lives");
                handler.BeginReceive(data => ReceiveCallback(data, handler));
                return;
            }
            LogData($"Touch detected: {string.Join(",", positions)}");
            ChnageColorToDevice(targetNoColor, td, handler);

            if (greenTilePos == td)
            {
                LogData($"Player {playerNumber} hit green Tile.");
                int newScore = Scores[playerNumber];
                int newLifeLine = LifeLines[playerNumber];
                newScore += (Level + LifeLines[playerNumber]) * 5; // increase score five times to regular increase
                newLifeLine = Math.Min(newLifeLine + 1, 5);
                updateScore(newScore, playerNumber);
                updateLifeline(newLifeLine, playerNumber);
                greenTilePos = -1;
            }
            else if (blueTilePos.Contains(td))
            {
                LogData($"Player {playerNumber} hit blue Tile.");
                int newScore = Scores[playerNumber];
                newScore += Level + LifeLines[playerNumber];
                updateScore(newScore, playerNumber);
                blueTilePos.Remove(td);
                if (blueTilePos.Count() == 0) IterationWon();
            }
            else if (redTilePos.Contains(td))
            {
                LogData($"Player {playerNumber} hit red Tile.");
                int newScore = Scores[playerNumber];
                int newLifeLine = LifeLines[playerNumber];
                newScore -= (Level + LifeLines[playerNumber]) * 2; // decrease score twice as much
                newLifeLine = Math.Max(newLifeLine - 1, 0);
                updateScore(newScore, playerNumber);
                updateLifeline(newLifeLine, playerNumber);
                redTilePos.Remove(td);
                GameContinue();
            }

            handler.activeDevices.Remove(td);
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    override protected void IterationLost(object state)
    {
        isGameRunning = false;
        udpHandlers.ForEach(x => x.StopReceive());
        if (!config.timerPointLoss && state == null)
        {
            IterationWon();
            return;
        }

        LogData($"iteration failed within {IterationTime} second");
        if (config.timerPointLoss)
            iterationTimer.Dispose();
        for (int i = 0; i<config.MaxPlayers; i++)
        {
            LifeLines[i] = Math.Max(LifeLines[i] - 1, 0);
        }
        Status = $"{GameStatus.Running} : Lost Lifeline by time LifeLine: {string.Join(", ", LifeLines)}";

        if (!IsLifeLineRemaining())
        {
            //TexttoSpeech: Oh no! You’ve lost all your lives. Game over! 🎮
            musicPlayer.Announcement("content/voicelines/GameOver.mp3", false);
            LogData("GAME OVER");
            EndGame();

        }
        else
        {
            RunGameInSequence();
        }
    }

    private void GameContinue()
    {
        if(!IsLifeLineRemaining())
        {
            //TexttoSpeech: Oh no! You’ve lost all your lives. Game over! 🎮
            musicPlayer.Announcement("content/voicelines/GameOver.mp3", false);
            LogData("GAME OVER");
            EndGame();
        } 
    }

    private bool IsLifeLineRemaining() 
    {
        foreach(var lifeLine in LifeLines)
        {
            if (lifeLine > 0)
            {
                return true;
            }
        }
        return false;
    }
}
