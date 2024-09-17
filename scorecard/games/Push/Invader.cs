using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Invader : BaseSingleDevice
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

    public Invader(GameConfig config) : base(config)
    {
        bulletPositions = new List<int>();
        homeTiles = new List<int>();
        hitTiles = new List<int>();
        totalTiles = handler.DeviceList.Count;
        columns = config.columns;
        bulletSpeedSlowdown = 1000; // Default slowdown (can be adjusted per level)
        targetPerPlayer = 3;  // Number of hit tiles per player, can increase with level
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
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    // Logic for each iteration
    protected override void OnIteration()
    {
        bulletPositions.Clear();
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
        int homeColumns = Level * 2;
        // Place home tiles in the first column
        for (int i = 0; i < totalTiles; i+=columns*2)
        {
            for (int j = 0; j < homeColumns; j++)
            {
                homeTiles.Add(i);
                if(i-j > 0) homeTiles.Add(i-j);
                if (i - j -1 > 0) homeTiles.Add(i - j - 1);
                if (i + j < totalTiles) homeTiles.Add(i+j);
            }
        }

        // Generate hit tiles randomly on the right side
        int hitTileCount = targetPerPlayer * config.MaxPlayers;

        while (hitTiles.Count < hitTileCount)
        {
            int randomHitTile = random.Next(totalTiles);

            // Ensure hit tile is far enough from home tiles (distance > 15)
            if (!hitTiles.Contains(randomHitTile) && !homeTiles.Any(tile => Math.Abs(randomHitTile - tile) < 15))
            {
                hitTiles.Add(randomHitTile);  // Add unique hit tiles
                bulletPositions.Add(MoveBullet(randomHitTile));
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
            handler.DeviceList[tile] = homeColor;
        }
        foreach (int tile in hitTiles)
        {
            handler.DeviceList[tile] = hitColor;
        }
        foreach (int tile in bulletPositions)
        {
            handler.DeviceList[tile] = bulletColor;
        }
        handler.SendColorsToUdp(handler.DeviceList);
    }

    // Move bullets one step closer to the home tiles
    private async void MoveBullets(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && isGameRunning)
            {
                List<int> nextBulletPositions = new List<int>();
                foreach (int pos in bulletPositions)
                {
                    nextBulletPositions.Add(MoveBullet(pos));
                }
                bulletPositions.Clear();
                bulletPositions.AddRange(nextBulletPositions);

                // Send updated bullet positions to devices
                handler.SendColorsToUdp(handler.DeviceList);

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

    private int MoveBullet(int position)
    {
        handler.DeviceList[position] = ColorPaletteone.NoColor;
        int newPos = (position / columns) % 2 == 0 ? position - 1 : position + 1;
        if(homeTiles.Contains(newPos))
        {
            logger.Log("bullet hit home. Iteration lost");
            udpHandlers.ForEach(x => x.activeDevices.Clear());
            CancelTargetThread();
            BlinkAllAsync(1);
            IterationLost("bullet hit home. Iteration lost");
        }
        handler.DeviceList[position] = hitTiles.Contains(position) ? hitColor:backgroundColor;
        handler.DeviceList[newPos] = bulletColor;
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
            if (bulletPositions.Contains(tileIndex))
            {
                bulletPositions.Remove(tileIndex);  // Remove the bullet
                handler.DeviceList[tileIndex] = hitTiles.Contains(tileIndex) ? hitColor: backgroundColor;
                updateScore(Score + Level);
                generateBullet();
                bulletsRemaining--;  // Decrease remaining bullet count
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
