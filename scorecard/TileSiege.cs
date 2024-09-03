using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using scorecard;
using scorecard.lib;

public class TileSiege : BaseMultiDevice
{
    private int targetTilesPerPlayer = 10;
    private int safeZoneSize = 4;
    private List<int> obstaclePositions = new List<int>(); // Define obstaclePositions

    public TileSiege(GameConfig config, int targetTilesPerPlayer) : base(config)
    {
        this.targetTilesPerPlayer = targetTilesPerPlayer;
    }

    protected override void Initialize()
    {
        // Show initial animation to prepare players
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        //if (killerLineTask == null || killerLineTask.IsCompleted)
        //{
        //    if (killerLineTask != null && !killerLineTask.IsCompleted)
        //    {
        //        logger.Log("killer line task still running");
        //    }
        //    logger.Log("Starting killer line task");
        //    killerLineTask = Task.Run(() => drawkillingline(null));
        //}
        Task.Run(() => PlayCountdown((IterationTime/1000)-1));
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
    private async void  PlayCountdown(int toNumber) {
        for (int i = toNumber; i > 0 ; i--)
        {
            if (!isGameRunning)
                break;
            musicPlayer.PlayEffect($"content/numbers/{NumberToWordConverter.Convert(i)}.mp3");
            await Task.Delay(100);
        }
    }
    protected override void OnIteration()
    {
        ifSafeZoneTrgToStart = false;
        SendSameColorToAllDevice(ColorPaletteone.NoColor, true);

        CreateSafeZones();
        CreateTargetTiles();

        SendColorToUdpAsync();
    }

    #region setting targets
    private void CreateSafeZones()
    {
        int totalSafeZones = 0;

        while (totalSafeZones < config.MaxPlayers)
        {
            int origMain = random.Next(0, deviceMapping.Count - 1);

            while (!IsValidSafeZonePosition(origMain))
            {
                origMain = random.Next(0, deviceMapping.Count - 1);
            }

            var group = GetSafeZoneGroup(origMain);
            obstaclePositions.AddRange(group);

            List<int> ActualGroup = new List<int>();

            foreach (var item in group)
            {
                if (base.deviceMapping.ContainsKey(item))
                    ActualGroup.Add(base.deviceMapping[item].deviceNo);
            }
            foreach (var item in group)
            {
                int actualHandlerPos = base.deviceMapping[item].deviceNo;
                base.deviceMapping[item].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Green;
                base.deviceMapping[item].udpHandler.activeDevicesGroup.Add(actualHandlerPos, ActualGroup);
                base.deviceMapping[item].isActive = true;
            }
            LogData($"Safe Zone created at positions: {string.Join(",", obstaclePositions)}");
            totalSafeZones++;
        }
    }

    private void CreateTargetTiles()
    {
       
        int targetTilesPerPlayerlocal = (int)config.MaxPlayers * this.targetTilesPerPlayer/ udpHandlers.Count;
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
            int totalTargets = 0;

            while (totalTargets < targetTilesPerPlayerlocal)
            {
                int randomTile = random.Next(0, handler.DeviceList.Count-1);

                while (handler.activeDevices.Contains(randomTile) || IsSafeZoneTile(randomTile))
                {
                    randomTile = random.Next(0, handler.DeviceList.Count - 1);
                }

                handler.activeDevices.Add(randomTile);
                handler.DeviceList[randomTile] = ColorPaletteone.Red;
                //  deviceMapping[randomTile].udpHandler.activeDevices.Add(deviceMapping[randomTile].deviceNo);
                //  deviceMapping[randomTile].udpHandler.DeviceList[deviceMapping[randomTile].deviceNo] = ColorPaletteone.Red;
                totalTargets++;
            }
            logger.Log($"Active devices filling handler:{handler.name} active devices: {string.Join(",", handler.activeDevices)}");
        }

        

    }

    private bool IsSafeZoneTile(int pos)
    {
        return obstaclePositions.Contains(pos);
    }

    private bool IsValidSafeZonePosition(int pos)
    {
        var device = base.deviceMapping[pos];
        int lastRowMax = device.udpHandler.DeviceList.Count;
        int lastRowMin = lastRowMax - config.columns;

        if ((device.deviceNo <= lastRowMax && device.deviceNo > lastRowMin) || device.deviceNo % config.columns == config.columns - 1)
        {
            return false;
        }

        foreach (int x in obstaclePositions)
        {
            List<int> surroundingTiles = surroundingMap[x];
            if (surroundingTiles.Contains(pos))
            {
                return false;
            }
        }

        return true;
    }

    private List<int> GetSafeZoneGroup(int origMain)
    {
        int nextPosition = 1;
        int nextRowAdd = config.columns;

        if ((origMain % config.columns == 0 && origMain != 0) || origMain == rows * config.columns)
        {
            nextPosition = -1;
        }
        if (deviceMapping.Count - origMain < config.columns)
        {
            nextRowAdd = -1 * nextRowAdd;
        }

        int mainRight = origMain + nextPosition;
        int mainBelow = origMain + nextRowAdd;
        int mainBelowRight = mainBelow + nextPosition;

        return new List<int> { origMain, mainRight, mainBelow, mainBelowRight };
    }
    #endregion

    bool ifSafeZoneTrgToStart = false;
    #region datareceiving and results
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

            List<int> targetHits = new List<int>();

            if (ifSafeZoneTrgToStart)
            {
                List<int> l2 = new List<int>();
                foreach (var position in positions)
                {
                    if (handler.activeDevicesGroup.ContainsKey(position))
                    {
                        l2.AddRange(handler.activeDevicesGroup[position]);
                    }
                }
                if (l2.Count > 0)
                {
                    LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
                    LogData($"Touch detected: {string.Join(",", l2)}");
                    ChnageColorToDevice(ColorPaletteone.NoColor, l2, handler);
                    updateScore(Score + l2.Count / 4);
                    foreach (var item in l2)
                    {
                        handler.activeDevicesGroup.Remove(item);
                    }
                    LogData($"Score updated: {Score} active:{string.Join(",", handler.activeDevicesGroup)}");
                }

            }
            else
            {
                #region hunting targets
                foreach (var position in positions)
                {
                    if (handler.activeDevices.Contains(position))
                    {
                        targetHits.Add(position);
                    }
                }

                if (targetHits.Count > 0)
                {
                    LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
                    LogData($"Tiles hit: {string.Join(",", targetHits)}");

                    ChnageColorToDevice(ColorPaletteone.NoColor, targetHits, handler);
                    updateScore(Score + targetHits.Count);
                    foreach (var target in targetHits)
                    {
                        handler.activeDevices.Remove(target);
                    }
                }

                if (udpHandlers.All(x => x.activeDevices.Count == 0))
                {
                    // All targets are cleared; players must now reach the safe zone
                    Status = GameStatus.ReachSafeZone;
                    LogData("All targets cleared. Players must reach the safe zone!");
                    ifSafeZoneTrgToStart = true;
                    //base.IterationWon();
                    //return;
                }
                #endregion
            }

        if (udpHandlers.Where(x => x.activeDevicesGroup.Count > 0).Count() == 0)
             IterationWon();        
        else
            handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
    #endregion
}
