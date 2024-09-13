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

    int noofPatterns = 1;
    private Dictionary<string, string[]> pattern = new Dictionary<string, string[]>();

    public PatternBuilderGame(GameConfig config, int noofPatterns) : base(config)
    {
      this.noofPatterns = noofPatterns;
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

        musicPlayer.PlayEffect("content/PatternIntro.wav");
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
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
            IterationWon();
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
    string baseColor = ColorPaletteone.Red;
    protected override void OnIteration() 
    {
        
        SendSameColorToAllDevice(baseColor, true);
        BlinkAllAsync(1);
       
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
        }

        List<int> newActiveIndices = new List<int>();
        while (newActiveIndices.Count < noofPatterns)
        {
            var pattern = SelectRandomPattern(random.Next(0, config.columns - 5), random.Next(0, rows - 5), config.columns);
            newActiveIndices.AddRange(pattern);
        }

     
        UpdateGrid(newActiveIndices);
        SendColorToUdpAsync();
        Thread.Sleep(5000);
        SendSameColorToAllDevice(baseColor, false);
    }
    private void UpdateGrid(List<int> newActiveIndices)
    {
         foreach (int pos in newActiveIndices)
        {
            int actualHandlerPos = base.deviceMapping[pos].deviceNo;
            base.deviceMapping[pos].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Green;
            base.deviceMapping[pos].udpHandler.activeDevices.Add(actualHandlerPos);            
        }
    }

    private void PlacePattern(UdpHandler handler, List<int> pattern)
    {
        foreach (var tileIndex in pattern)
        {
            handler.DeviceList[tileIndex] = targetColor;
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
      //  if(row * columns + column>139)
      //      LogData($"row:{row} column:{column} index:{row * columns + column} x:{x} y:{y}");
        return row * columns + column;
    }


}
