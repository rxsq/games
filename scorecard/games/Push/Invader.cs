using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Invador : BaseMultiDevice
{
    private string bulletColor = ColorPaletteone.Silver;
    private string homeColor = ColorPaletteone.Green;
    private string hitColor = ColorPaletteone.Red;
    private string backgroundColor = ColorPaletteone.NoColor;
    private List<int> bulletPositions;  // Tiles where bullets are launched
    private List<int> homeTiles;    // Tiles to defend
    private List<int> hitTiles;     // Hit tiles that launch bullets
    private int bulletSpeedSlowdown;  // Slowdown to control bullet speed
    private int targetPerPlayer;     // Number of hit tiles for each player
    private int columns;
    private int totalTiles;
    private int bulletsPerLevel = 15;  // Each player will have 15 bullets per level
    private int bulletsRemaining;
    private CancellationTokenSource cancellationTokenSource;
    private CoolDown coolDown;

    public Invador(GameConfig config) : base(config)
    {
        bulletPositions = new List<int>();
        homeTiles = new List<int>();
        hitTiles = new List<int>();
        totalTiles = deviceMapping.Count;
        columns = config.columns;
        bulletSpeedSlowdown = 1000; // Default slowdown (can be adjusted per level)
        targetPerPlayer = 3;  // Number of hit tiles per player, can increase with level
        coolDown = new CoolDown();
    }

    protected override void Initialize()
    {
        base.BlinkAllAsync(1);
        logger.Log("Game Initialized");
    }

    protected override void StartAnimition()
    {
        base.StartAnimition();
    }

    // Start receiving tile data when the game starts
    protected override void OnStart()
    {
        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    // Logic for each iteration
    protected override void OnIteration()
    {
        coolDown.SetFlagTrue(100);
        bulletPositions.Clear();
        foreach (var handler in udpHandlers)
        {
            handler.activeDevices.Clear();
        }
        SendColorToDevices(backgroundColor, true); // Set all tiles to a base color at the start
        bulletsRemaining = bulletsPerLevel * config.MaxPlayers;
        GenerateHomeAndHitTiles();
        DisplayTiles();
        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => MoveBullets(cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    // This method generates the home tiles and hit tiles based on level and number of players
    private void GenerateHomeAndHitTiles()
    {
        homeTiles.Clear();
        hitTiles.Clear();
        int noHomeTiles = Level * columns;
        // Place home tiles in the first column
        for (int i = 0; i < noHomeTiles; i++)
        {
            homeTiles.Add(i);
        }

        // Generate hit tiles randomly on the right side
        int hitTileCount = targetPerPlayer * config.MaxPlayers;

        while (hitTiles.Count < hitTileCount)
        {
            int randomHitTile = random.Next(totalTiles);

            // Ensure hit tile is far enough from home tiles (distance > 15)
            if (!hitTiles.Contains(randomHitTile) && !homeTiles.Any(tile => Math.Abs(randomHitTile - tile) < 105))
            {
                hitTiles.Add(randomHitTile);  // Add unique hit tiles
                int newBulletPosition = MoveBullet(randomHitTile);
                int newBulletActualPosition = deviceMapping[newBulletPosition].deviceNo;
                deviceMapping[newBulletPosition].udpHandler.activeDevices.Add(newBulletActualPosition);
                bulletPositions.Add(newBulletPosition);
            }
        }
    }
    
    protected void generateBullet()
    {
        bulletPositions.Add(hitTiles[random.Next(hitTiles.Count)]);
    }

    private void DisplayTiles()
    {
        foreach (int tile in homeTiles)
        {
            int actualPos = deviceMapping[tile].deviceNo;
            deviceMapping[tile].udpHandler.DeviceList[actualPos] = homeColor;
        }
        foreach (int tile in hitTiles)
        {
            int actualPos = deviceMapping[tile].deviceNo;
            deviceMapping[tile].udpHandler.DeviceList[actualPos] = hitColor;
        }
        //foreach (int tile in bulletPositions)
        //{
        //    int actualPos = deviceMapping[tile].deviceNo;
        //    deviceMapping[tile].udpHandler.DeviceList[tile] = bulletColor;
        //}
        foreach(var handler in udpHandlers)
        {
            foreach(int pos in handler.activeDevices)
            {
                handler.DeviceList[pos] = bulletColor;
            }
            handler.SendColorsToUdp(handler.DeviceList);
        }
    }

    // Move bullets one step closer to the home tiles
    private async void MoveBullets(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && isGameRunning)
            {
                List<int> nextBulletPositions = new List<int>();
                foreach(var handler in udpHandlers)
                {
                    handler.activeDevices.Clear();
                }
                foreach (int pos in bulletPositions)
                {
                    int newBulletPos = MoveBullet(pos);
                    int actualNewBulletPos = deviceMapping[newBulletPos].deviceNo;
                    deviceMapping[newBulletPos].udpHandler.activeDevices.Add(actualNewBulletPos);
                    nextBulletPositions.Add(newBulletPos);
                }
                bulletPositions.Clear();
                bulletPositions.AddRange(nextBulletPositions);

                foreach(var handler in udpHandlers)
                {
                    // Send updated bullet positions to devices
                    await handler.SendColorsToUdpAsync(handler.DeviceList);
                }

                await Task.Delay(bulletSpeedSlowdown, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Task was cancelled, exit gracefully
            logger.Log("Bullet movement task was canceled.");
        }
        catch (Exception ex)
        {
            // Handle any unexpected exceptions
            logger.Log($"Error in bullet movement: {ex.Message}");
        }
    }

    //private int MoveBullet(int position)
    //{
    //    handler.DeviceList[position] = ColorPaletteone.NoColor;
    //    int newPos = (position / columns) % 2 == 0 ? position - 1 : position + 1;
    //    if(homeTiles.Contains(newPos))
    //    {
    //        logger.Log("bullet hit home. Iteration lost");
    //        udpHandlers.ForEach(x => x.activeDevices.Clear());
    //        CancelTargetThread();
    //        BlinkAllAsync(1);
    //        IterationLost("bullet hit home. Iteration lost");
    //    }
    //    handler.DeviceList[position] = hitTiles.Contains(position) ? hitColor:backgroundColor;
    //    handler.DeviceList[newPos] = bulletColor;
    //    return newPos;
    //}
    private int MoveBullet(int position)
    {
        int actualPosition = deviceMapping[position].deviceNo;
        //deviceMapping[position].udpHandler.DeviceList[actualPosition] = backgroundColor;
        //handler.DeviceList[position] = ColorPaletteone.NoColor;
        //int quo = position / columns;
        //int rem = position % columns;
        //int newPos = quo*columns-rem-1;
        int newPos = position - 7;
        if (homeTiles.Contains(newPos))
        {
            logger.Log("bullet hit home. Iteration lost");
            udpHandlers.ForEach(x => x.activeDevices.Clear());
            CancelTargetThread();
            BlinkAllAsync(1);
            IterationLost("bullet hit home. Iteration lost");
        }
        int newActualPosition = deviceMapping[newPos].deviceNo;
        deviceMapping[position].udpHandler.DeviceList[actualPosition] = hitTiles.Contains(position) ? hitColor : backgroundColor;
        deviceMapping[newPos].udpHandler.DeviceList[newActualPosition] = bulletColor;
        return newPos;
    }
    // Callback for tile touches
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning) return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        // Parse the touched device positions
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        // Check if the tapped tile corresponds to a bullet
        foreach (int tileIndex in positions)
        {
            if (handler.activeDevices.Contains(tileIndex) && !coolDown.Flag)
            {
                int mappingDeviceNo = GetKeyFromDeviceMapping(handler, tileIndex);
                if(mappingDeviceNo>0)
                {
                    bulletPositions.Remove(mappingDeviceNo);  // Remove the bullet
                    handler.activeDevices.Remove(tileIndex);
                    handler.DeviceList[tileIndex] = hitTiles.Contains(tileIndex) ? hitColor : backgroundColor;
                    updateScore(Score + Level);
                    generateBullet();
                    bulletsRemaining--;  // Decrease remaining bullet count
                    coolDown.SetFlagTrue(100);
                }
                
            }
        }

        // Check if all bullets have been intercepted or time has run out
        if (bulletsRemaining <= 0)
        {
            logger.Log("All bullets secured. Iteration won!");
            bulletSpeedSlowdown = Math.Max(150, bulletSpeedSlowdown - 200);  // Decrease bullet slowdown time (speed up)
            updateScore(Score + 100*lifeLine);
            IterationWon();
            return;
        }

        // Continue receiving input
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }
    protected void CancelTargetThread()
    {
        cancellationTokenSource?.Cancel(); // Cancel the running task
        cancellationTokenSource?.Dispose(); // Dispose of the token source
        cancellationTokenSource = null;

    }
}
