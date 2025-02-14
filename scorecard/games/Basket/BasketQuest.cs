using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class BasketQuest : BaseMultiplayerGame
{
    string[] starColorSet;
    Dictionary<int, int> basketMap;
    List<int> hitTargets;
    string wrongTargetColor;
    CoolDown coolDown;
    int numberOfPlayers;
    public BasketQuest(GameConfig co) : base(co)
    {
        if (config.NoofLedPerdevice == 1) starColorSet = new string[] { ColorPaletteone.Red, ColorPaletteone.Green, ColorPaletteone.Blue, ColorPaletteone.White, ColorPaletteone.Yellow };
        else starColorSet = new string[] { ColorPalette.Red, ColorPalette.Green, ColorPalette.Blue, ColorPalette.White, ColorPalette.yellow };

        numberOfPlayers = co.MaxPlayers;
        basketMap = new Dictionary<int, int>();
        hitTargets = new List<int>();
        targetColor = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
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
        SetTarget();
    }
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private void SetTarget()
    {
        handler.activeDevices.Clear();
        basketMap.Clear();

        for (int i = 0; i < numberOfPlayers && handler.activeDevices.Count <= numberOfPlayers; i++)
        {
            int index;
            do
            {
                index = random.Next(0, handler.DeviceList.Count());
            } while (handler.activeDevices.Contains(index));

            handler.DeviceList[index] = starColorSet[i];
            handler.activeDevices.Add(index);
            basketMap[index] = i;
        }


        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            if (!handler.activeDevices.Contains(i))
            {
                handler.DeviceList[i] = config.NoofLedPerdevice==1 ? ColorPaletteone.NoColor : ColorPalette.noColor3;
            }
        }
        handler.SendColorsToUdp(handler.DeviceList);
    }


    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning && coolDown.Flag)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //   LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> touchedActiveDevices = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          //    .Where(x=> activeIndicesSingle.Contains((x.index  -2) / config.NoofLedPerdevice))
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();
        ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3, touchedActiveDevices, handler);
        foreach (int td in touchedActiveDevices)
        {
            if (isGameRunning && basketMap.ContainsKey(td))
            {
                handler.activeDevices.Remove(td);
                int playerNo = basketMap[td];
                updateScore(Scores[playerNo] + 1, playerNo);
                LogData($"Score updated: {string.Join(", ", Scores)}  position: {td}");
                IterationWon();
            }
            
        }
        handler.BeginReceive(data => ReceiveCallback(data, handler));

    }
}
