using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static log4net.Appender.ColoredConsoleAppender;

public class CyberShotArena : BaseMultiDevice
{
    private int totalTargets;
    private int targetsPerPlayer = 10;
    private CancellationTokenSource cancellationTokenSource;
    private CoolDown coolDown = new CoolDown();
    private List<int> targets = new List<int>();
    private int slowDown;
    private string backgroundColor;
    public CyberShotArena(GameConfig config) : base(config)
    { 
        targetColor = config.NoofLedPerdevice == 3 ? ColorPalette.Green : ColorPaletteone.Green;
        totalTargets = config.MaxPlayers * targetsPerPlayer;
        backgroundColor = config.NoofLedPerdevice == 3 ? ColorPalette.noColor3 : ColorPaletteone.NoColor;
    }
    protected override void Initialize()
    {
        
    }
    protected override void OnStart()
    {
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
        cancellationTokenSource = new CancellationTokenSource();
        //SwapPositions(cancellationTokenSource.Token);
    }
    protected override void OnIteration()
    {
        coolDown.SetFlagTrue(100);
        SendColorToDevices(backgroundColor, true);
        cancellationTokenSource = new CancellationTokenSource();
        targets.Clear();
        slowDown = 5000 - ((level-1)*1000);
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
        }
        ActivateRandomLights();
        
    }
    private void ActivateRandomLights()
    {
        SendColorToDevices(backgroundColor, true);
        while (targets.Count < totalTargets)
        {
            int randomTarget = random.Next(deviceMapping.Count);

            // Ensure hit tile is far enough from home tiles (distance > 15)
            if (!targets.Contains(randomTarget))
            {
                targets.Add(randomTarget);  // Add unique hit tiles
                int newBulletActualPosition = deviceMapping[randomTarget].deviceNo;
                deviceMapping[randomTarget].udpHandler.activeDevices.Add(newBulletActualPosition);
                deviceMapping[randomTarget].udpHandler.DeviceList[newBulletActualPosition] = targetColor;
            }
        }
        foreach (var handler in udpHandlers)
        {
            handler.SendColorsToUdp(handler.DeviceList);
        }
    }
    //private async void SwapPositions(CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        while (!cancellationToken.IsCancellationRequested)
    //        {
    //            if (!isGameRunning) continue;
    //            await Task.Delay(slowDown, cancellationToken);
    //            SendColorToDevices(backgroundColor, false);
    //            targets.Clear();
    //            foreach (var handler in udpHandlers)
    //            {
    //                handler.activeDevices.Clear();
    //            }
    //            List<int> newTargets = new List<int>();
    //            while (targets.Count < totalTargets)
    //            {
    //                int randomTarget = random.Next(deviceMapping.Count);

    //                // Ensure hit tile is far enough from home tiles (distance > 15)
    //                if (!newTargets.Contains(randomTarget))
    //                {
    //                    newTargets.Add(randomTarget);  // Add unique hit tiles
    //                    int newBulletActualPosition = deviceMapping[randomTarget].deviceNo;
    //                    deviceMapping[randomTarget].udpHandler.activeDevices.Add(newBulletActualPosition);
    //                    deviceMapping[randomTarget].udpHandler.DeviceList[newBulletActualPosition] = targetColor;
    //                }
    //            }
    //            targets = newTargets;
    //            foreach (var handler in udpHandlers)
    //            {
    //                handler.SendColorsToUdp(handler.DeviceList);
    //            }
                
    //        }
    //    }
    //    catch (TaskCanceledException)
    //    {
    //        // Task was cancelled, exit gracefully
    //        logger.Log("Bullet movement task was canceled.");
    //    }
    //    catch (Exception ex)
    //    {
    //        // Handle any unexpected exceptions
    //        logger.Log($"Error in bullet movement: {ex.Message}");
    //    }
    //}

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)

    {
        if (!isGameRunning)
            return;
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        //LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();
        LogData($"Received data from {String.Join(",", positions)}: active positions:{string.Join(",", handler.activeDevices)}");
        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning)
                return;
            foreach (var device in touchedActiveDevices) 
            { 
                handler.DeviceList[device] = ColorPaletteone.NoColor; 
                handler.activeDevices.Remove(device);
                targets.Remove(GetKeyFromDeviceMapping(handler, device));
            }
            handler.SendColorsToUdp(handler.DeviceList);
            updateScore(Score + Level + LifeLine);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
        }



        if (targets.Count() == 0)
        {

            IterationWon();
        }
        else
        {

            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }

    }
}
