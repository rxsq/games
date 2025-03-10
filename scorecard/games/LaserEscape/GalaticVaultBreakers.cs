using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class GalaticVaultBreakers : BaseSingleDevice
{
    CoolDown coolDown;
    LaserEscapeHandler laserEscapeHandler;
    int iterationScore = 0;
    string activeColor = ColorPaletteone.Green;
    string touchedColor = ColorPaletteone.Red;
    int iterationCount = 1;
    public GalaticVaultBreakers(GameConfig co) : base(co)
    {
        coolDown = new CoolDown();
        laserEscapeHandler = new LaserEscapeHandler(ConfigurationSettings.AppSettings["LaserControllerComPort"], 96, 2, 6);
    }
    protected override void Initialize()
    {
        base.BlinkAllAsync(2);
    }
    protected override async void StartAnimition()
    {
        LoopAll();
        base.StartAnimition();
    }

    protected override void OnIteration()
    {
        coolDown.SetFlagTrue(500);
        handler.activeDevices.Clear();
        ActivateLasers();
    }
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
        laserEscapeHandler.BeginReceive(cutLasers => ReceiveCallBackLaser(cutLasers));
    }

    private void ActivateLasers()
    {
        handler.SendColorsToUdp(handler.DeviceList);
        laserEscapeHandler.ActivateLevel(Level);
    }


    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //   LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> touchedActiveDevices = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();
        foreach(var device in touchedActiveDevices)
        {
            if(handler.activeDevices.Contains(device))
            {
                handler.activeDevices.Remove(device);
                handler.DeviceList[device] = touchedColor;
                handler.SendColorsToUdp(handler.DeviceList);
            }
        }
        if(handler.activeDevices.Count()==0)
        {
            updateScore(iterationScore*lifeLine);
            iterationCount++;
            IterationWon();
        }
    }
    private void ReceiveCallBackLaser(List<int> cutLasers)
    {
        if (!isGameRunning)
            return;
        iterationScore -= cutLasers.Count;
        if (Score <= 0)
        {
            iterationCount++;
            IterationLost(null);
        }
        ;
    }
}
