using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class Chaser : BaseSingleDevice
{
    string gamecolor;
    private int currentTarget;

    public Chaser(GameConfig config) : base(config)
    {
       
    }

    protected override void Initialize()
    {
       
        base.BlinkAllAsync(2);
    }

    protected override void OnIteration()
    {
        gamecolor = gameColors[random.Next(gameColors.Count - 1)];
        ActivateNextLight();
    }

    protected override void OnStart()
    {
        base.BlinkAllAsync(1);
        OnIteration();
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private void ActivateNextLight()
    {
        // Clear all lights
        for (int i = 0; i < devices.Count(); i++)
        {
            devices[i] = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
        }

        // Activate the next target light
        currentTarget = random.Next(devices.Count());
        devices[currentTarget] = gamecolor;

        handler.SendColorsToUdp(devices);
    }

    int blinktime = 0;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Where(x => (x.index - 2) / config.NoofLedPerdevice == currentTarget)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        if (positions.Count > 0)
        {
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, positions, handler);
            updateScore( base.Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)}");
            MoveToNextIteration();
        }
        else
        {
            LogData($"no touch found position {String.Join(",", positions)}");
        }

        if (positions.Count == 0)
        {
            blinktime++;
            if (blinktime > 40)
            {
                LogData($"moving target");
                ActivateNextLight();
                blinktime = 0;
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

   
}
