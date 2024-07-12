using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public class FloorGame : BaseMultiDevice
{
    private int killerSpeedReduction = 200;
    private System.Threading.Timer gameTimer;
    private Dictionary<UdpHandler, List<int>> killerRowsDict = new Dictionary<UdpHandler, List<int>>();
    private Dictionary<UdpHandler, Dictionary<int, List<int>>> activedevicesGroup = new Dictionary<UdpHandler, Dictionary<int, List<int>>>();

    public FloorGame(GameConfig config, int killerSpeedReduction) : base(config)
    {
        this.killerSpeedReduction = killerSpeedReduction;
    }

    protected override void Initialize()
    {
        AnimateColor(false);
        AnimateColor(true);
        AnimateGrowth(ColorPaletteone.Blue);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        if (gameTimer == null)
        {
            gameTimer = new System.Threading.Timer(timerSet, null, 1000, 500000000); // Change target tiles every 10 seconds
        }

        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }

    protected override void OnIteration()
    {
        SendSameColorToAllDevice(ColorPaletteone.Red, true);
        targetColor = ColorPaletteone.Green;
        int totalTargets = 0;

        activedevicesGroup.Clear();
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
        }

        while (totalTargets < config.MaxPlayers)
        {
            foreach (var handler in udpHandlers)
            {
                if (totalTargets >= config.MaxPlayers)
                    break;

                int origMain = random.Next((handler.DeviceList.Count - handler.columns) / 2) * 2;
                int main = origMain;

                while (handler.activeDevices.Contains(main) || (origMain / handler.columns) % 2 == 1)
                {
                    origMain = random.Next((handler.DeviceList.Count - handler.columns) / 2) * 2;
                    main = origMain;
                }

                int mainRight = origMain + 1;
                int mainBelow = Resequencer(origMain + handler.columns, handler);
                int mainBelowRight = Resequencer(origMain + handler.columns + 1, handler);

                handler.activeDevices.Add(main);
                handler.activeDevices.Add(mainRight);
                handler.activeDevices.Add(mainBelow);
                handler.activeDevices.Add(mainBelowRight);

                LogData($"Active devices filling handler:{handler.name} active devices: {string.Join(",", handler.activeDevices)}");

                if (!activedevicesGroup.ContainsKey(handler))
                {
                    activedevicesGroup[handler] = new Dictionary<int, List<int>>();
                }

                var group = new List<int> { main, mainRight, mainBelow, mainBelowRight };
                activedevicesGroup[handler].Add(main, group);
                activedevicesGroup[handler].Add(mainRight, group);
                activedevicesGroup[handler].Add(mainBelow, group);
                activedevicesGroup[handler].Add(mainBelowRight, group);

                totalTargets++;
                base.ChnageColorToDevice(targetColor, group, handler);
            }
        }
    }

    protected void timerSet(object state)
    {
        if (!isGameRunning)
        {
            gameTimer = null;
            return;
        }

        foreach (var handler in udpHandlers)
        {
            for (int row = 0; row < handler.Rows; row++)
            {
                var colorList = new List<string>();
                var cl = handlerDevices[handler].Select(x => x).ToList();
                int rowNum = (row / handler.Rows) % 2 == 0 ? (row % handler.Rows) : handler.Rows - 1 - (row % handler.Rows);
                var blueLineDevices = new List<int>();

                for (int i = 0; i < handler.columns; i++)
                {
                    if (handler.activeDevices.Contains(rowNum * handler.columns + i))
                        continue;

                    cl[rowNum * handler.columns + i] = ColorPaletteone.Blue;
                    blueLineDevices.Add(rowNum * handler.columns + i);
                }

                if (!isGameRunning)
                {
                    gameTimer = null;
                    return;
                }
                killerRowsDict.Clear();
                handler.SendColorsToUdp(cl);
                killerRowsDict.Add(handler, blueLineDevices);
                LogData($"filling data handler row:{row} handler:{handler.name} active:{string.Join(",", handler.activeDevices)} blueline: {string.Join(",", blueLineDevices)}");

                Thread.Sleep(1000 - (base.level - 1) * killerSpeedReduction);
            }

            handler.SendColorsToUdp(handlerDevices[handler]);
        }

        if (isGameRunning)
        {
            timerSet(null);
        }
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

        if (positions.Count > 0)
        {
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Touch detected: {string.Join(",", positions)}");

            var touchedPos = activedevicesGroup[handler]
                .Where(x => positions.Contains(x.Key))
                .SelectMany(x => x.Value)
                .ToList();

            if (touchedPos.Count > 0)
            {
                LogData("Color change detected");
                ChnageColorToDevice(ColorPaletteone.NoColor, touchedPos, handler);

                foreach (var pos in touchedPos)
                {
                    activedevicesGroup[handler].Remove(pos);
                }

                updateScore(Score + touchedPos.Count / 4);
                LogData($"Score updated: {Score}");
            }
            else if (killerRowsDict.ContainsKey(handler) && positions.Any(x => killerRowsDict[handler].Contains(x)))
            {
                isGameRunning = false;
                musicPlayer.PlayEffect("content/you failed.mp3");
                LogData($"Game Failed : {Score} position:{string.Join(",", positions)} killerRow : {string.Join(",", killerRowsDict[handler])}");
                killerRowsDict[handler].Clear();
                base.Score--;
                TargetTimeElapsed(null);
                return;
            }
        }

        LogData($"{handler.name} processing received data");

        if (activedevicesGroup.Values.All(x => x.Count == 0))
        {
            if (isGameRunning)
            {
                MoveToNextIteration();
            }
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    
}
