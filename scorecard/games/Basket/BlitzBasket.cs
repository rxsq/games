using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class BlitzBasket:BaseSingleDevice
{
    int targetCount;
    List<int> targets;
    List<int> hitTargets;
    string wrongTargetColor;
    CoolDown coolDown;
    public BlitzBasket(GameConfig co):base(co)
    {
        targetCount = 3;
        targets = new List<int>();
        hitTargets = new List<int>();
        targetColor  = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
        wrongTargetColor = config.NoofLedPerdevice == 3 ? ColorPalette.Red : ColorPaletteone.Red;
        coolDown = new CoolDown();
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
        ActivateRandomLights();
    }
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private void ActivateRandomLights()
    {
        hitTargets.Clear();
        targets = new List<int>();
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.activeDevices.Add(i);
            handler.DeviceList[i] = wrongTargetColor;
        }

        while (targets.Count < targetCount)
        {
            int index = random.Next(handler.DeviceList.Count());
            if (!targets.Contains(index))
            {
                handler.DeviceList[index] = targetColor; // Green indicates the target light
                targets.Add(index);
            }
        }

        handler.SendColorsToUdp(handler.DeviceList);

    }


    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning )
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //   LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> touchedActiveDevices = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          //    .Where(x=> activeIndicesSingle.Contains((x.index  -2) / config.NoofLedPerdevice))
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        if (touchedActiveDevices.Count > 0 && !coolDown.Flag)
        {
            if (!isGameRunning)
                return;
            if(!coolDown.Flag)
            {
                foreach (var device in touchedActiveDevices)
                {
                    if (targets.Contains(device))
                    {
                        ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, touchedActiveDevices, handler);
                        handler.activeDevices.Remove(device);
                        targets.Remove(device);
                        hitTargets.Add(device);
                        updateScore(Score + Level);
                        LogData($"Score updated: {Score}.");
                    }
                    else if (!hitTargets.Contains(device))
                    {
                        coolDown.SetFlagTrue(500);
                        IterationLost(null);
                        LogData($"Wrong Basket.");
                        return;
                    }
                }
            }
        }

        if (targets.Count == 0)
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
