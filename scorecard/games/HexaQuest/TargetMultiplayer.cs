using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TargetMultiplayer : BaseMultiplayerGame
{
    private int[] starIndex;
    private int numberOfPlayers;
    private Dictionary<int, List<int>> targetMap;

    public TargetMultiplayer(GameConfig config, int[] starIndex) : base(config)
    {
        logger.Log(string.Join(", ", starIndex));

        // Initialize starIndex properly
        this.starIndex = new int[starIndex.Length];
        starIndex.CopyTo(this.starIndex, 0);

        this.numberOfPlayers = config.MaxPlayers;
        targetMap = new Dictionary<int, List<int>>();
    }
    protected override async void StartAnimition()
    {
        LoopAll();
        base.StartAnimition();

    }
    protected override void Initialize()
    {
        BlinkAllAsync(2);
    }

    Task targetTask;
    protected override void OnStart()
    {
        if (targetTask == null || targetTask.IsCompleted)
        {
            if (targetTask != null && !targetTask.IsCompleted)
            {
                logger.Log("targetTask task still running");
            }
            logger.Log("Starting targetTask task");
            targetTask = Task.Run(() => blinkTargetLight());
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    protected override void OnIteration()
    {
        SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.PinkCyanMagenta);
        BlinkAllAsync(1);
        SetTarget();
    }
    private void blinkTargetLight()
    {
        if (!isGameRunning)
            return;

        BlinkLights(handler.activeDevices, 1, handler, config.NoofLedPerdevice == 1 ? ColorPaletteone.Blue : ColorPalette.Blue);
        if (isGameRunning)
        {
            Thread.Sleep(2000);
            blinkTargetLight();
        }
    }
    private string[] GetStarColor()
    {
        string[] starColor = new string[numberOfPlayers];
        for(int i = 0; i < numberOfPlayers; i++)
        {
            int index;
            string color;
            do
            {
                index = random.Next(gameColors.Count - 1);
                color = gameColors[index];
            } while(starColor.Contains(color));
            starColor[i] = color;
            handler.DeviceList[starIndex[i]] = color;
        }
        return starColor;
    }

    private void SetTarget()
    {
        handler.activeDevices.Clear();
        string[] starColor = GetStarColor();
        int numberOfStarColorDevices = 2;

        for (int i = 0; i < numberOfPlayers; i++) 
        {
            List<int> targets = new List<int>();
            for (int j = 0; j < numberOfStarColorDevices; j++)
            {
                int index;
                do
                {
                    index = random.Next(0, handler.DeviceList.Count());
                } while (handler.activeDevices.Contains(index) || starIndex.Contains(index) || index == 30);

                handler.DeviceList[index] = starColor[i];
                handler.activeDevices.Add(index);
                targets.Add(index);
            }
            targetMap[i] = targets;
        }
        //make sure star is colord


        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            if (!handler.activeDevices.Contains(i) && !starIndex.Contains(i))
            {

                Console.WriteLine(i.ToString());
                string newColor;
                do
                {
                    //newColor = ColorPalette.Blue;
                    newColor = gameColors[random.Next(gameColors.Count - 1)];
                } while (starColor.Contains(newColor));

                handler.DeviceList[i] = newColor;
            }
            else { Console.WriteLine("Target at:" + i.ToString()); }
        }
        handler.SendColorsToUdp(handler.DeviceList);
    }

    int blinktime = 0;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .Where(position => position >= 0)
                                          .ToList();



        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        foreach (int td in touchedActiveDevices) 
        {
            LogData($"Touch detected: {string.Join(",", positions)}");
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, td, handler);

            for (int i = 0; i < targetMap.Count(); i++)
            {
                if (targetMap[i].Contains(td))
                {
                    int newScore = Scores[i];
                    newScore+=Level;
                    updateScore(newScore, i);
                    LogData($"Score updated: {Score.ToString()}  position: {td}");
                    targetMap[i].Remove(td);
                    if (targetMap[i].Count() == 0)
                    {
                        IterationWon();
                        handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
                        return;
                    }
                }
            }
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
}
