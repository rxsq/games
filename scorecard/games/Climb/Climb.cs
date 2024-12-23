using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;

public class Climb : BaseGameClimb
{
    private double targetPercentage;
    private int targetCount;
    string gamecolor;
    public Climb(GameConfig config) : base(config)
    {
        targetCount = (int)Math.Round(config.MaxPlayers * 1.5);
        //LoopAll(ColorPaletteone.NoColor, 2);
    }
    protected override void Initialize()
    {
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }
    protected void BlinkAllAsync(int nooftimes)
    {

        for (int i = 0; i < nooftimes; i++)
        {
            var tasks = new List<Task>();
            
            var colors = handler.DeviceList.Select(x => config.NoofLedPerdevice == 1 ? ColorPaletteone.Yellow : ColorPalette.yellow).ToList();
            handler.SendColorsToUdp(colors);
            Thread.Sleep(100);
            handler.SendColorsToUdpAsync(handler.DeviceList);
            Thread.Sleep(100);
        }
    }
    protected override async void StartAnimition()
    {
      //  if (handler == null)
       //     handler = new UdpHandlerWeTop(config.IpAddress, config.LocalPort, config.RemotePort, config.SocketBReceiverPort, config.NoofLedPerdevice, config.columns, "handler-1");

       
       // base.StartAnimition();

    }
    //protected void LoopAll(string basecolor, int frequency)
    //{
    //    for (int i = 0; i < frequency; i++)
    //    {
           
    //            var deepCopiedList = handler.DeviceList.Select(x => basecolor).ToList();
    //            var loopColor = gameColors[random.Next(gameColors.Count - 1)];
    //            for (int j = 0; j < handler.DeviceList.Count; j++)
    //            {
    //                deepCopiedList[j] = loopColor;
    //                handler.SendColorsToUdp(deepCopiedList);
    //                Thread.Sleep(100);
    //                deepCopiedList[j] = basecolor;
    //                handler.SendColorsToUdp(deepCopiedList);
    //                Thread.Sleep(100);
    //            }

    //            LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
    //        }
        
    //}
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

   

    private void ActivateRandomLights()
    {


        targetCount = this.config.MaxPlayers * 2;
        // Clear all lights
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.DeviceList[i] = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Yellow;
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


    private void ReceiveCallback(byte[] receivedBytes, UdpHandlerWeTop handler)

    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
        
        List<int> positions = receivedBytes
                                .Select((b, index) => new { Byte = b, Index = index }) // Select byte and index
                                .Where(x => x.Byte == 0xCC) // Filter where byte equals 0xCC
                                .Select(x => x.Index-3) // Select only the indices
                                .ToList();
        LogData($"Received data from {String.Join(",", positions)}: active positions:{string.Join(",", handler.activeDevices)}");
        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning)
                return;
            foreach (var device in touchedActiveDevices) { handler.DeviceList[device] = ColorPaletteone.NoColor; }
            handler.SendColorsToUdp(handler.DeviceList);       
            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
        }



        if (handler.activeDevices.Count() == 0)
        {
            int random = new Random().Next(0, 9);

            IterationWon();
        }
        else
        {

            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

    }


}
