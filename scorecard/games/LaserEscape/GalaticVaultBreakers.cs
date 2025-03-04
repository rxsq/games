using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class GalaticVaultBreakers : BaseSingleDevice
{
    CoolDown coolDown;
    public GalaticVaultBreakers(GameConfig co) : base(co)
    {
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
        handler.SendColorsToUdp(handler.DeviceList);
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

    }
}
