using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class Smash : BaseSingleDevice
{



    private double targetPercentage;
    private int targetCount;
    string gamecolor;
    
    public Smash(GameConfig config, double targetPercentage) : base(config)
    {
        this.targetPercentage = targetPercentage;
    }
    protected override void Initialize()
    {
       targetCount = (int)(devices.Count() * targetPercentage);
       base.BlinkAllAsync(2);
    }

   
    protected override void OnIteration()
    {
        gamecolor = gameColors[random.Next(gameColors.Count - 1)];
        ActivateRandomLights();

    }
    protected override void OnStart()
    {

        musicPlayer.PlayEffect("content/SmashIntro.wav");
        base.BlinkAllAsync(1);
        OnIteration();
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private void ActivateRandomLights()
    {
       
        targetCount = (int)(devices.Count() * targetPercentage);
        // Clear all lights
        for (int i = 0; i < devices.Count(); i++)
        {
            devices[i] = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
        }

        // Activate a percentage of random lights as targets
        activeIndices[handler].Clear();
       
        while (activeIndices[handler].Count < targetCount)
        {
            int index = random.Next(devices.Count());
            if (!activeIndices[handler].Contains(index))
            {
                devices[index] = gamecolor; // Green indicates the target light
                activeIndices[handler].Add(index);
            }
        }

        handler.SendColorsToUdp(devices);
       
    }

    int blinktime = 0;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)

    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Where(x=> activeIndicesSingle.Contains((x.index  -2) / config.NoofLedPerdevice))
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        if (positions.Count > 0)
        {
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, positions, handler);
            activeIndices[handler].RemoveWhere(x=> positions.Contains(x));
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",",positions)} active positions:{string.Join(",",activeIndices[handler])}");
        }
        else
        {
            LogData($"no touch found position {String.Join(",",positions)} active positions:{string.Join(",",activeIndices[handler])}");
        }


        if (activeIndices.Values.Where(x => x.Count > 0).Count() == 0)
        {
            MoveToNextIteration();
        }
        else
        {
            blinktime++;
            if (blinktime > 200)
            {
                LogData($"moving target");
                ActivateRandomLights();
                blinktime = 0;
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

    }


}
