using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class FloorIsLavaGame : BaseGame
{
    private int lavaInterval;
    private int lavaDuration;
    private List<int> lavaTiles = new List<int>();
    private TTSHelper ttsHelper;

    public FloorIsLavaGame(GameConfig config, int lavaInterval, int lavaDuration, string ttsCredentialsPath) : base(config)
    {
        this.lavaInterval = lavaInterval;
        this.lavaDuration = lavaDuration;
        ttsHelper = new TTSHelper(ttsCredentialsPath);
    }

    protected override void Initialize()
    {
        ttsHelper.SpeakText("The game is starting. Get ready!");
        BlinkAllAsync(2); // Initial blink to indicate game start
    }

    protected override void OnStart()
    {
       
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
    protected override void OnIteration()
    {
        GenerateLavaTiles();
        base.OnIteration();
    }

    protected override void OnEnd()
    {
        ttsHelper.SpeakText($"Game over. Your final score is {Score}. Well played!");
        base.OnEnd();
    }

    private void GenerateLavaTiles()
    {
        if (!isGameRunning) return;

        foreach (var handler in udpHandlers)
        {
            lavaTiles.Clear();
            var lavaIndices = new HashSet<int>();

            for (int i = 0; i < 10; i++)
            {
                int index;
                do
                {
                    index = random.Next(handler.DeviceList.Count);
                } while (lavaIndices.Contains(index));

                lavaIndices.Add(index);
                lavaTiles.Add(index);
            }

            foreach (var index in lavaTiles)
            {
                handlerDevices[handler][index] = ColorPaletteone.Red;
            }

            handler.SendColorsToUdp(handlerDevices[handler]);
            LogData($"Lava tiles: {string.Join(",", lavaTiles)}");

            ttsHelper.SpeakText($"Watch out! Lava is appearing on tiles {string.Join(", ", lavaTiles)}.");

            // Reset lava tiles after a duration
            Thread.Sleep(lavaDuration);
            foreach (var index in lavaTiles)
            {
                handlerDevices[handler][index] = ColorPaletteone.NoColor;
            }

            handler.SendColorsToUdp(handlerDevices[handler]);
        }
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
      

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

        if (positions.Count > 0)
        {
            LogData($"Touch detected: {string.Join(",", positions)}");

            if (positions.Any(pos => lavaTiles.Contains(pos)))
            {
                isGameRunning = false;
                ttsHelper.SpeakText("Game over! You stepped on lava.");
                LogData($"Game Over: Player stepped on lava at positions: {string.Join(",", positions)}");
                LifeLine -= 1;
                TargetTimeElapsed(null);
                return;
            }
        }

        LogData($"{handler.name} processing received data");
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
}
