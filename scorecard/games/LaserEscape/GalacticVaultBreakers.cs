using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class GalacticVaultBreakers : BaseSingleDevice
{
    CoolDown coolDown;
    LaserEscapeHandler laserEscapeHandler;
    int iterationScore = 0;
    string activeColor = ColorPaletteone.Green;
    string touchedColor = ColorPaletteone.Red;
    int iterationCount = 1;
    int iterationLives = 5;
    public GalacticVaultBreakers(GameConfig co) : base(co)
    {
        coolDown = new CoolDown();
        laserEscapeHandler = new LaserEscapeHandler(ConfigurationSettings.AppSettings["LaserControllerComPort"], 96, 2, 6, ReceiveCallBackLaser);
    }
    protected override void Initialize()
    {
        //base.BlinkAllAsync(2);
    }
    protected override async void StartAnimition()
    {
        //LoopAll();
        //base.StartAnimition();
    }

    protected override void OnIteration()
    {
        iterationLives = 5;
        laserEscapeHandler.StopReceive();
        coolDown.SetFlagTrue(2000);
        handler.activeDevices.Clear();
        ActivatePush();
        //laserEscapeHandler.MakePattern();
        ActivateLasers();
        Thread.Sleep(2000);
        laserEscapeHandler.StartReceive();

    }


    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
        //laserEscapeHandler.BeginReceive(cutLasers => ReceiveCallBackLaser(cutLasers));
    }

    private void ActivateLasers()
    {

        iterationScore = ActivateLevel(Level);
    }
    private void ActivatePush()
    {
        for (int i = 0; i < config.MaxPlayers; i++)
        {
            if (iterationCount % 2 == 0)
            {
                handler.DeviceList[i] = activeColor;
                handler.activeDevices.Add(i);
            } else
            {
                handler.DeviceList[i + 5] = activeColor;
                handler.activeDevices.Add(i + 5);
            }
        }
        handler.SendColorsToUdp(handler.DeviceList);
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!coolDown.Flag && isGameRunning)
        {
            string receivedData = Encoding.UTF8.GetString(receivedBytes);
            //   LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

            List<int> touchedActiveDevices = receivedData.Select((value, index) => new { value, index })
                                              .Where(x => x.value == 0x0A)
                                              .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                              .ToList();
            foreach (var device in touchedActiveDevices)
            {
                if (handler.activeDevices.Contains(device))
                {
                    handler.activeDevices.Remove(device);
                    handler.DeviceList[device] = touchedColor;
                    handler.SendColorsToUdp(handler.DeviceList);
                }
            }
            if (handler.activeDevices.Count() == 0)
            {
                updateScore(iterationScore * lifeLine);
                iterationCount++;
                laserEscapeHandler.StopReceive();
                IterationWon();
            }
            else
            {
                handler.BeginReceive(data => ReceiveCallback(data, handler));
            }
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
    private void ReceiveCallBackLaser(List<int> cutLasers)
    {
        if (!coolDown.Flag && isGameRunning)
        {
            foreach (int lase in cutLasers)
            {
                if (laserEscapeHandler.activeDevices.Contains(lase))
                {
                    laserEscapeHandler.activeDevices.Remove(lase);
                    logger.Log("Laser cut: " + lase);
                    iterationScore--;
                    iterationLives--;
                    
                    if (iterationLives <= 0)
                    {
                        iterationCount++;
                        laserEscapeHandler.StopReceive();
                        IterationLost(null);
                    }
                }
            }

        }
    }
    public int ActivateLevel(int level)
    {
        laserEscapeHandler.TurnOffAllTheLasers(); // Ensure previous state is cleared
        int activatedLasers = 0;

        if (level == 3)
        {
            // Turn on first row with alternating lasers
            for (int col = 0; col < laserEscapeHandler.columns; col += 2)
            {
                int laserIndex = (col * laserEscapeHandler.rows);
                laserEscapeHandler.SetLaserState(laserIndex, true);

                laserIndex = (col * laserEscapeHandler.rows + 1);
                laserEscapeHandler.SetLaserState(laserIndex, true);

                activatedLasers += 2;
            }
        }
        else if (level == 1)
        {
            // Turn on all laserEscapeHandler.rows except the first three
            for (int row = 3; row < laserEscapeHandler.rows; row++)
            {
                laserEscapeHandler.TurnOnRow(row);
                activatedLasers += laserEscapeHandler.columns;
            }
        }
        else if (level == 5)
        {
            // Turn on first row with alternating lasers
            for (int col = 0; col < laserEscapeHandler.columns; col += 3)
            {
                int laserIndex = (col * laserEscapeHandler.rows);
                laserEscapeHandler.SetLaserState(laserIndex, true);

                laserIndex = (col * laserEscapeHandler.rows + 1);
                laserEscapeHandler.SetLaserState(laserIndex, true);

                laserIndex = (col * laserEscapeHandler.rows + 2);
                laserEscapeHandler.SetLaserState(laserIndex, true);

                activatedLasers += 3;
            }
        }
        else if (level == 4)
        {
            // Turn on all laserEscapeHandler.rows except the first 2
            for (int row = 2; row < laserEscapeHandler.rows; row++)
            {
                laserEscapeHandler.TurnOnRow(row);
                activatedLasers += laserEscapeHandler.columns;
            }
        }
        else if (level == 2)
        {
            // First 2 lasers in the first 2 columns
            for (int col = 0; col < 2; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    int laserIndex = (col * laserEscapeHandler.rows) + row;
                    laserEscapeHandler.SetLaserState(laserIndex, true);
                    activatedLasers++;
                }
            }

            // Last 4 lasers in the next 4 columns, repeating pattern with a 3-column gap
            for (int col = 4; col < laserEscapeHandler.columns; col += 7) // Start from 2nd col, 3-column gap
            {
                for (int repeat = 0; repeat < 4; repeat++) // 4 columns in each repeat
                {
                    int currentCol = col + repeat;
                    if (currentCol < laserEscapeHandler.columns)
                    {
                        for (int row = laserEscapeHandler.rows - 4; row < laserEscapeHandler.rows; row++) // Last 4 laserEscapeHandler.rows
                        {
                            int laserIndex = (currentCol * laserEscapeHandler.rows) + row;
                            laserEscapeHandler.SetLaserState(laserIndex, true);
                            activatedLasers++;
                        }
                    }
                }
            }
        }

        laserEscapeHandler.SendData();
        Thread.Sleep(1000);
        laserEscapeHandler.StartReceive();
        logger.Log($"Activated level {level}, total lasers activated: {activatedLasers}");

        return activatedLasers;
    }
    protected override void IterationLost(object state)
    {
        laserEscapeHandler.StopReceive();
        base.IterationLost(state);

    }
}
