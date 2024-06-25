using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class PatternBuilderGame : BaseMultiDevice
{
  
    private Random random = new Random();
    private Dictionary<UdpHandler, List<string>> handlerDevices;

    private static readonly List<string[]> letterPatterns = new List<string[]>
    {
        new string[]
        {
            "x, y", "x, y+1", "x, y+2", "x, y+3", "x+1, y", "x+1, y+3", "x+2, y", "x+2, y+3", "x+3, y", "x+3, y+1", "x+3, y+2", "x+3, y+3"
        }, // Letter "A"
        // Add more letter patterns
    };

    private static readonly List<string[]> shapePatterns = new List<string[]>
    {
        new string[]
        {
            "x, y", "x, y+1", "x, y+2", "x, y+3", "x+1, y", "x+1, y+3", "x+2, y", "x+2, y+3", "x+3, y", "x+3, y+1", "x+3, y+2", "x+3, y+3"
        }, // Square
        // Add more shape patterns
    };
   

    public PatternBuilderGame(GameConfig config, int gridRows) : base(config)
    {
        rows = gridRows;
        handlerDevices = new Dictionary<UdpHandler, List<string>>();

        foreach (var handler in udpHandlers)
        {
            handler.ColumnCount = handler.DeviceList.Count / rows;
            handlerDevices[handler] = new List<string>(new string[rows * handler.ColumnCount]);
        }
    }

    protected override void Initialize()
    {
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        OnIteration();
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();

        foreach (int position in positions)
        {
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Touch detected: {string.Join(",", positions)}");
            if (activeIndices[handler].Contains(position))
            {
                LogData("Color change detected");
                base.ChnageColorToDevice(ColorPaletteone.NoColor, position, handler);
               
                activeIndices[handler].Remove(position);
                base.Score = base.Score + 1;
                LogData($"Score updated: {Score}");
            }
        }

//        if (activeIndices.Values.All(x => x.Count == 0))
//        {
//            BlinkAllAsync(2);
//            MoveToNextIteration();
//        }
//        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnIteration() 
    {
        foreach (var handler in udpHandlers)
        {
            activeIndices[handler].Clear();
            for (int i = 0; i < handlerDevices[handler].Count; i++)
            {
                handlerDevices[handler][i] = ColorPaletteone.Red;
                // ResequencedPositions(handlerDevices[handler], handler)[i] = ColorPaletteone.Red;
            }
        }

        targetColor = ColorPaletteone.Green;
        int totalTargets = config.MaxPlayers;
        int targetsPerHandler = totalTargets / udpHandlers.Count;
        int extraTargets = totalTargets % udpHandlers.Count;

        foreach (var handler in udpHandlers)
        {
            while (activeIndices[handler].Count < targetsPerHandler + (extraTargets > 0 ? 1 : 0))
            {
                var pattern = SelectRandomPattern();
                //PlacePattern(handler, pattern);
                foreach (var index in pattern)
                {
                    activeIndices[handler].Add(index);
                    handlerDevices[handler][index] = targetColor;
                }
            }

            extraTargets = Math.Max(0, extraTargets - 1);
            //   handler.SendColorsToUdp(handlerDevices[handler]);
            handler.SendColorsToUdp(ResequencedPositions(handlerDevices[handler], handler));
           
           
        }
        string x = "";
    }


    private void PlacePattern(UdpHandler handler, List<int> pattern)
    {
        foreach (var tileIndex in pattern)
        {
            handlerDevices[handler][tileIndex] = targetColor;
        }
    }

    private List<int> SelectRandomPattern()
    {
        var allPatterns = letterPatterns.Concat(shapePatterns).ToList();
        int randomIndex = random.Next(allPatterns.Count);
        var selectedPattern = allPatterns[randomIndex];
        return selectedPattern.Select(tile => ConvertToIndex(tile, 0, 0)).ToList();
    }

    private int ConvertToIndex(string tile, int x, int y)
    {
        int row;
        int column;
        var parts = tile.Split(',');
        var pattern = @"x(\+|-)?(\d+),\s*y(\+|-)?(\d+)";
        var match = Regex.Match(tile, pattern);
        //if (int.TryParse(parts[0], out row) == false){
        //    var segments = parts[0].Split('+');
        //    Console.WriteLine(string.Join(",", segments[0]));
        //    if (segments[0] == "x") {row = x + int.Parse(segments[1]);}
        //    else if (segments[0] == "y") {row = y + int.Parse(segments[1]);}

        //}
        row = int.Parse(parts[0]);
        column = int.Parse(parts[1]);
        return row * rows + column;
    }


}
