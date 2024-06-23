using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Target : BaseSingleDevice
{
    //his is to hold previous star color so that its not get duplicated
    protected HashSet<int> usedStarIndices1 = new HashSet<int>();
    private int starIndex = 18;
    public Target(GameConfig config, int starIndex) : base(config)
    {
        this.starIndex = starIndex;
        //if(istest)
        //    this.colors = new List<string> { ColorPaletteone.Pink, ColorPaletteone.Purple, ColorPaletteone.Navy, ColorPaletteone.Yellow, ColorPaletteone.Coral, ColorPaletteone.White, ColorPaletteone.Cyan };
    }
    protected override void Initialize()
    {
        base.SendDataToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Silver : ColorPalette.SilverGrayWhite, starIndex);
        LoopAll(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3,1);
        BlinkAllAsync(2);
    }

    protected override void OnStart()
    {
        //base.BlinkLights(new HashSet<int> { starIndex },2, handler);
        OnIteration();
        handler.BeginReceive(data => ReceiveCallback(data, handler));
       
    }

    protected override void OnIteration()
    {
         SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.PinkCyanMagenta);
        BlinkAllAsync(1);
         SetColorsOfDevices();
       
    }

    private string GetStarColor()
    {
        int index;
        do
        {
            index = random.Next(gameColors.Count -1 );
        } while (usedStarIndices1.Contains(index));
        usedStarIndices1.Add(index);

        string starColor = gameColors[index];
        //handlerDevices[handler][starIndex] = starColor;
        return starColor;
    }





    int blinktime = 0;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();

        LogData($"Touch detected: {string.Join(",", positions)}");

        foreach (int position in positions)
        {
            int actualPos = position / config.NoofLedPerdevice;
            if (activeIndices[handler].Contains(actualPos))
            {
                LogData("Color change detected");
                musicPlayer.PlayEfeect("content/target_hit.mp3");
                ChnageColorToDevice(config.NoofLedPerdevice==1? ColorPaletteone.NoColor:ColorPalette.noColor3, actualPos, handler);
                activeIndicesSingle.Remove(actualPos);
                base.Score = base.Score + 1;
                LogData($"Score updated: {Score}");
            }
        }
       
        if (activeIndices.Values.Where(x => x.Count > 0).Count() == 0)
        {
            MoveToNextIteration();
        }
        else
        {
            blinktime++;
            if (blinktime > 30)
            {
                BlinkLights(activeIndicesSingle, 1, handler);
                blinktime = 0;
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
       
    }

    private void SetColorsOfDevices()
    {
        activeIndicesSingle.Clear();
        string starColor = GetStarColor();
        int numberOfStarColorDevices = (int)Math.Round(devices.Count() * 0.3);
        // HashSet<int> usedIndices = new HashSet<int> { starIndex };
       
        for (int i = 0; i < numberOfStarColorDevices; i++)
        {
            int index;
            do
            {
                index = random.Next(devices.Count());
            } while (activeIndicesSingle.Contains(index));

            devices[index] = starColor;
            activeIndicesSingle.Add(index);
        }
        //make sure star is colord
       

        for (int i = 0; i < devices.Count(); i++)
        {
            if (!activeIndicesSingle.Contains(i))
            {
                string newColor;
                do
                {
                    newColor = gameColors[random.Next(gameColors.Count-1)];
                } while (newColor == starColor);

                devices[i] = newColor;
            }
        }
        activeIndicesSingle.Remove(starIndex);

        devices[starIndex] = starColor;
        handler.SendColorsToUdp(devices);
      // activeIndicesSingle = usedIndices;
        LogData($"Sending final colors: {string.Join(",", devices)}");
        LogData($"Sending star color: {devices[starIndex]}");
    }
}
