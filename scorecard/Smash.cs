using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Smash : BaseSingleDevice
{



    private double targetPercentage;
    private int targetCount;
    string gamecolor;
    
    public Smash(GameConfig config) : base(config)
    {
        this.config.MaxPlayers = 1;
    }
    protected override void Initialize()
    {
       targetCount = (int)Math.Round(config.MaxPlayers * 1.5);
       base.BlinkAllAsync(2);
    }

   
    protected override void OnIteration()
    {
        gamecolor = gameColors[random.Next(gameColors.Count - 1)];
        ActivateRandomLights();

    }
    protected override void OnStart()
    {

        // musicPlayer.PlayEffect("content/SmashIntro.wav");
        handler.BeginReceive(data => ReceiveCallback(data, handler));
        //Task.Run(() => MoveTargetLight());
    }

    //private void MoveTargetLight()
    //{
    //    if (!isGameRunning)
    //        return;
        
    //    if (handler.activeDevices.Count > 0)
    //    {
    //        TargetTimeElapsed(null);
    //        return;
    //    }
    //    ActivateRandomLights();
    //    if (isGameRunning)
    //    {
    //        Thread.Sleep((IterationTime/ 4)); // this number to be reduces as per speed

    //        MoveTargetLight();
    //    }
    //}

    private void ActivateRandomLights()
    {
        

            targetCount = this.config.MaxPlayers * 2;
        // Clear all lights
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.DeviceList[i] = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
        }

        // Activate a percentage of random lights as targets
        handler.activeDevices.Clear();
       
        while (handler.activeDevices.Count < targetCount)
        {
            int index = random.Next(handler.DeviceList.Count());
            if (!handler.activeDevices.Contains(index))
            {
                handler.DeviceList[index] = gamecolor; // Green indicates the target light
                handler.activeDevices.Add(index);
            }
        }

        handler.SendColorsToUdp(handler.DeviceList);
       
    }

    
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)

    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
     //   LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                      //    .Where(x=> activeIndicesSingle.Contains((x.index  -2) / config.NoofLedPerdevice))
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning)
                return;
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, touchedActiveDevices, handler);
            handler.activeDevices.RemoveAll(x=> touchedActiveDevices.Contains(x));
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",",positions)} active positions:{string.Join(",",handler.activeDevices)}");
        }
        


        if (handler.activeDevices.Count() == 0)
        {
            int random = new Random().Next(0, 9);
                      
            MoveToNextIteration();
        }
        else
        {
             
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

    }


}
