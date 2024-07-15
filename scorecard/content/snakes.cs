using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class Snakes : BaseMultiDevice
{
    private int lavaInterval;
    private int lavaDuration;
    int counter = 0;

    private Dictionary<UdpHandler, Dictionary<int, List<int>>> activeSnakes = new Dictionary<UdpHandler, Dictionary<int, List<int>>>();
    private List<int> lavaTiles = new List<int>();

    public Snakes(GameConfig config, int lavaInterval, int lavaDuration, string ttsCredentialsPath) : base(config)
    {
        this.lavaInterval = lavaInterval;
        this.lavaDuration = lavaDuration;
    }

    protected override void Initialize()
    {
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
        GenerateSnakes();
        base.OnIteration();
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }

    private void GenerateSnakes()
    {
        foreach (var handler in udpHandlers)
        {
            if (!activeSnakes.ContainsKey(handler))
            {
                activeSnakes[handler] = new Dictionary<int, List<int>>();
            }
            int pos1 = random.Next(0, handler.DeviceList.Count - 4);
            int pos2 = pos1 + 1;
            int pos3 = pos2 + 1;
            int pos4 = pos3 + 1;
            var currentSnake = new List<int> { pos1, pos2, pos3, pos4 };
            activeSnakes[handler].Add(pos1, currentSnake);
            activeSnakes[handler].Add(pos2, currentSnake);
            activeSnakes[handler].Add(pos3, currentSnake);
            activeSnakes[handler].Add(pos4, currentSnake);
            foreach (var index in currentSnake)
            {
                handlerDevices[handler][index] = ColorPaletteone.Red;
            }

            handler.SendColorsToUdp(handlerDevices[handler]);
            Console.WriteLine("REACHED CHECKPOINT");
        }
    }
    private void MoveSnakes()
    {
        foreach (var handler in udpHandlers)
        {
            var newActiveSnakes = new Dictionary<int, List<int>>();
            foreach (var snakeEntry in activeSnakes[handler].ToList())
            {
                var snake = snakeEntry.Value;
                handlerDevices[handler][snake[0]] = ColorPaletteone.NoColor;

                // Shift snake positions
                for (int i = 0; i < snake.Count - 1; i++)
                {
                    snake[i] = snake[i + 1];
                }

                // Calculate new head position
                int newHeadPos = snake[snake.Count - 1] + 1;

                // Ensure new head position is within the range
                if (newHeadPos >= handler.DeviceList.Count)
                {
                    // If the new head position is out of range, handle the case (e.g., wrap around or stop)
                    newHeadPos = 0; // Example: wrap around to the beginning
                }

                snake[snake.Count - 1] = newHeadPos;

                // Update new active snakes dictionary
                foreach (var pos in snake)
                {
                    newActiveSnakes[pos] = snake;
                }

                // Update device colors
                foreach (var pos in snake)
                {
                    if (pos >= 0 && pos < handlerDevices[handler].Count)
                    {
                        handlerDevices[handler][pos] = ColorPaletteone.Red;
                    }
                }
            }

            // Replace old active snakes with new positions
            activeSnakes[handler] = newActiveSnakes;
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
        counter++;
        if (counter > 300)
        {
            MoveSnakes();
            Console.WriteLine("CHECKPOINT 2");
            counter = 0;
        }
        if (positions.Count > 0)
        {
            LogData($"Touch detected: {string.Join(",", positions)}");

            var touchedPos = activeSnakes[handler]
                .Where(x => positions.Contains(x.Key))
                .SelectMany(x => x.Value)
                .ToList();

            if (touchedPos.Count > 0)
            {
                LogData("Color change detected");
                ChnageColorToDevice(ColorPaletteone.NoColor, touchedPos, handler);

                foreach (var pos in touchedPos)
                {
                    activeSnakes[handler].Remove(pos);
                }

                updateScore(Score + touchedPos.Count / 4);
                LogData($"Score updated: {Score}");
            }
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
}
