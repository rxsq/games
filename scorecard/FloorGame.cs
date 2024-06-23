using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class FloorGame : BaseGame
{
   // private List<string> colors = new List<string> { ColorPaletteone.Green, ColorPaletteone.Red, ColorPaletteone.Blue };
    private int rows = 2;



    public FloorGame(GameConfig config, int gridRows) : base(config)
    {
        rows = gridRows;
        foreach (var handler in udpHandlers)
        {
            handler.ColumnCount = handler.DeviceList.Count / rows;
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
                ChnageColorToDevice(ColorPaletteone.NoColor, position, handler);
                activeIndices[handler].Remove(position);
            //    if (activeIndices[handler].Count == 0)
            //        activeIndices.Remove(handler);
                base.Score = base.Score + 1;
                LogData($"Score updated: {Score}");
            }
        }

        if (activeIndices.Values.Where(x=> x.Count>0).Count()==0)
        {
            BlinkAllAsync(2);
            MoveToNextIteration();
        }
        else
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
                int index = random.Next(handlerDevices[handler].Count);
                if (!activeIndices[handler].Contains(index))
                {
                    handlerDevices[handler][index] = targetColor;
                    activeIndices[handler].Add(index);
                }
            }

            extraTargets = Math.Max(0, extraTargets - 1);
            handler.SendColorsToUdp(handlerDevices[handler]);
        }
    }

   

    private void AnimateColor(bool reverse)
    {
        for (int iterations = 0; iterations < rows; iterations++)
        {
            foreach (var handler in udpHandlers)
            {
                for (int i = 0; i < handlerDevices[handler].Count; i++)
                {
                    handlerDevices[handler][i] = ColorPaletteone.NoColor;
                }

                int row = (iterations / handler.ColumnCount) % 2 == 0 ? (iterations % handler.ColumnCount) : 3 - (iterations % handler.ColumnCount);

                if (reverse)
                {
                    row = rows - row - 1;
                }

                for (int i = 0; i < handler.ColumnCount; i++)
                {
                    handlerDevices[handler][row * handler.ColumnCount + i] = ColorPaletteone.Green;
                }

                handler.SendColorsToUdp(handlerDevices[handler]);
                Thread.Sleep(100);
            }
        }
    }

   
    
}
