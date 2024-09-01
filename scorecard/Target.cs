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
    private List<int> starIndices = new List<int>();
   
    public Target(GameConfig config, int starIndex) : base(config)
    {
        //this.config.MaxPlayers = 3;
        this.starIndex = starIndex;
        //if(istest)
        //    this.colors = new List<string> { ColorPaletteone.Pink, ColorPaletteone.Purple, ColorPaletteone.Navy, ColorPaletteone.Yellow, ColorPaletteone.Coral, ColorPaletteone.White, ColorPaletteone.Cyan };
    }
    protected override void Initialize()
    {
        var handler = udpHandlers[0];
      //  musicPlayer.PlayEffect("content/TargetIntro.wav");
        base.SendDataToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Silver : ColorPalette.SilverGrayWhite, starIndex);
        //LoopAll(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3,1);
        BlinkAllAsync(2);
    }

    Task targetTask;
    protected override void OnStart()
    {
        //base.BlinkLights(new HashSet<int> { starIndex },2, handler);

      
        if (targetTask == null || targetTask.IsCompleted)
        {
            if (targetTask != null && !targetTask.IsCompleted)
            {
                logger.Log("targetTask task still running");
            }
            logger.Log("Starting targetTask task");
            targetTask = Task.Run(() => blinkTargetLight());
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));


    }

    protected override void OnIteration()
    {
         SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.PinkCyanMagenta);
         BlinkAllAsync(1);
         SetTarget();

        
    }
    private void blinkTargetLight()
    {
        if (!isGameRunning)
            return;

        BlinkLights(handler.activeDevices, 1, handler, config.NoofLedPerdevice == 1 ? ColorPaletteone.Blue : ColorPalette.Blue);
        if (isGameRunning)
        {
            Thread.Sleep(2000);
            blinkTargetLight();
        }
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
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .Where(position => position >= 0)
                                          .ToList();

        

        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            LogData($"Touch detected: {string.Join(",", positions)}");
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, touchedActiveDevices, handler);
            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
        }

        if (handler.activeDevices.Count() == 0)
        {
            IterationWon();
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

    }

    private void SetTarget()
    {
        handler.activeDevices.Clear();
        string starColor = GetStarColor();
        int numberOfStarColorDevices = config.MaxPlayers * 2;
        // HashSet<int> usedIndices = new HashSet<int> { starIndex };
     
        for (int i = 0; i < numberOfStarColorDevices; i++)
        {
            int index;
            do
            {
                index = random.Next(0,handler.DeviceList.Count());
            } while (handler.activeDevices.Contains(index) && index != 30);

            handler.DeviceList[index] = starColor;
            handler.activeDevices.Add(index);
        }
        //make sure star is colord
       

        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            if (!handler.activeDevices.Contains(i))
            {

                Console.WriteLine(i.ToString());
                string newColor;
                do
                {
                    //newColor = ColorPalette.Blue;
                    newColor = gameColors[random.Next(gameColors.Count-1)];
                } while (newColor == starColor);

                handler.DeviceList[i] = newColor;
            }
            else  { Console.WriteLine("Target at:" + i.ToString()); }
        }
        handler.activeDevices.Remove(starIndex);

        handler.DeviceList[starIndex] = starColor;
        handler.SendColorsToUdp(handler.DeviceList);
      // handler.activeDevices = usedIndices;
        //LogData($"Sending final colors: {string.Join(",", devices)}");
        //LogData($"Sending star color: {devices[starIndex]}");
    }
}
