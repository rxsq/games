using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class PatternBuilderGame : BaseMultiDevice
{
  
    private Random random = new Random();
    private Dictionary<string, string[]> pattern = new Dictionary<string, string[]>();

    //private static readonly List<string[]> letterPatterns = new List<string[]>
    //{
    //  //  new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "1, 0", "1, 3", "2, 0","2, 3", "3, 0", "3, 1", "3, 2", "3, 3"},
    //    //new string[]{"0, 0", "1, 0", "2, 0", "3, 0"},
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 1", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "1, 4", "2, 0", "2, 4", "3, 0", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 2", "1, 2", "2, 2", "3, 2", "4, 2" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 3", "1, 0", "1, 3", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 3", "4, 3" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 0", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 0" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "1, 4", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 0", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    new string[]{"0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "1, 4", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" },
    //    // number 9
    //    // Add more letter patterns
    //};

    //private static readonly List<string[]> shapePatterns = new List<string[]>
    //{
    //    new string[]
    //    {
    //        "0, 0", "0, 1", "0, 2", "0, 3", "1, 0", "1, 3", "2, 0", "2, 3", "3, 0", "3, 1", "3, 2", "3, 3"
    //    }, // Square
    //    // Add more shape patterns
    //};
   

    public PatternBuilderGame(GameConfig config) : base(config)
    {
      
    }

    protected override void Initialize()
    {
        pattern.Add("A", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 1", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("B", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 1", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("1", new string[] { "0, 2", "1, 2", "2, 2", "3, 2", "4, 2" });
        pattern.Add("2", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("3", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("4", new string[] { "0, 0", "0, 3", "1, 0", "1, 3", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 3", "4, 3" });
        pattern.Add("5", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("6", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 0", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("7", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 4", "2, 3", "3, 2", "4, 0" });
        pattern.Add("8", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "1, 4", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 0", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("9", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "0, 4", "1, 0", "1, 4", "2, 0", "2, 1", "2, 2", "2, 3", "2, 4", "3, 4", "4, 0", "4, 1", "4, 2", "4, 3", "4, 4" });
        pattern.Add("square", new string[] { "0, 0", "0, 1", "0, 2", "0, 3", "1, 0", "1, 3", "2, 0", "2, 3", "3, 0", "3, 1", "3, 2", "3, 3" });
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(3);
    }

    protected override void OnStart()
    {
        OnIteration();
        isGameRunning = true;
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }
    int counter;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
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
            if (handler.activeDevices.Contains(position))
            {
                LogData("Color change detected");
               
                base.ChnageColorToDevice(ColorPaletteone.NoColor, position, handler);

                handler.activeDevices.Remove(position);
                updateScore(Score + 1);
                LogData($"Score updated: {Score} active:{string.Join(",",handler.activeDevices)}");
            }
        }
        counter++;
        if (udpHandlers.Where(x=> x.activeDevices.Count>0).Count()==0)
        {
            if (!isGameRunning)
                return;
            MoveToNextIteration();
        }
        else
        {
            if (counter > 1000)
            {
                //asyn method to blink all
                BlinkLights(handler.activeDevices.ToList(),2,handler, ColorPaletteone.Green);
                Console.WriteLine("Flashed");
                counter = 0;
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    protected override void OnIteration() 
    {
        SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.PinkCyanMagenta,true);
        BlinkAllAsync(1);
       

        targetColor = ColorPaletteone.Green;

        string basecolor = ColorPaletteone.Red;
        
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
            for (int i = 0; i < handlerDevices[handler].Count; i++)
            {
                handlerDevices[handler][i] = basecolor;
            }
            List<int> newActiveIndices = new List<int>();

           
            var pattern = SelectRandomPattern(random.Next(0,handler.columns-5), random.Next(0, handler.Rows-5), handler.columns);
            //PlacePattern(handler, pattern);
            foreach (var index in pattern)
            {
                newActiveIndices.Add(index);
                handlerDevices[handler][index] = targetColor;
            }
           
            handler.activeDevices.AddRange(newActiveIndices);
           
            LogData($"before change: {string.Join(",", newActiveIndices)}");
            handler.SendColorsToUdp(ResequencedPositions(handlerDevices[handler], handler));
            LogData($"after change: {string.Join(",", handler.activeDevices)}");
            // handler.SendColorsToUdp(handlerDevices[handler]);
            //handlerDevices[handler] = cl;

            //   handler.SendColorsToUdp(handlerDevices[handler]);
            //    handler.SendColorsToUdp(cl);


        }
        Thread.Sleep(2000);
        SendColorToDevices(basecolor, true);



    }


    private void PlacePattern(UdpHandler handler, List<int> pattern)
    {
        foreach (var tileIndex in pattern)
        {
            handlerDevices[handler][tileIndex] = targetColor;
        }
    }

    private List<int> SelectRandomPattern(int PosX, int PosY, int columns)
    {
        //        var allPatterns = letterPatterns.Concat(shapePatterns).ToList();
        int t= random.Next(pattern.Count);
        var selectedPattern = pattern.ElementAt(t).Value;
       // var selectedPattern = pattern["6"];
       LogData($"key: {pattern.ElementAt(t)} selectedPattern: {string.Join(",",selectedPattern)}");
        return selectedPattern.Select(tile => ConvertToIndex(tile, PosX, PosY, columns)).ToList();
       // return letterPatterns[0].ToList();
    }

    private int ConvertToIndex(string tile, int x, int y,   int columns)
    {
        
        var parts = tile.Split(',');
        var row = int.Parse(parts[0]) + y;
     var   column = int.Parse(parts[1]) + x;
        if(row * columns + column>139)
            LogData($"row:{row} column:{column} index:{row * columns + column} x:{x} y:{y}");
        return row * columns + column;
    }


}
