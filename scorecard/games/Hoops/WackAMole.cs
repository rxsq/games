using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

class Hoops : BaseSingleDevice
{
    private string initialMoleColor = ColorPaletteone.Yellow;
    private string midGameMoleColor = ColorPaletteone.Silver;
    private string endGameMoleColor = ColorPaletteone.White;
    private string hitColor = ColorPaletteone.Red;
    private string backgroundColor = ColorPaletteone.Blue;
    private List<int> molePositions;  // Tiles where moles are launched
    private int moleSpeedSlowdown;  // Slowdown to control mole speed
    private int targetPerPlayer=3;     // Number of hit tiles for each player
    private int columns;
    private int totalTiles;
    private int molesPerPlayer = 15;  // Each player will have 15 bullets per level
    private int molesRemaining;
    private int molesperLevel;
    private CancellationTokenSource cancellationTokenSource;
    public Hoops(GameConfig config) : base(config)
    {
        molePositions = new List<int>();
        moleSpeedSlowdown = 2000;
        columns = config.columns;
        totalTiles = handler.DeviceList.Count;
        molesperLevel = config.MaxPlayers * molesPerPlayer;
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
        molePositions.Clear();
        SendColorToDevices(backgroundColor, true); // Set all tiles to a base color at the start
        molesRemaining = molesperLevel;
        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => GenerateMoles(cancellationTokenSource.Token));
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

        // Check if the tapped tile corresponds to a mole
        foreach (int tileIndex in positions)
        {
            if (molePositions.Contains(tileIndex))
            {
                molePositions.Remove(tileIndex);  // Remove the mole
                handler.DeviceList[tileIndex] = hitColor;
                updateScore(Score + Level);
                molesRemaining--;  // Decrease remaining mole count
            }
            handler.SendColorsToUdp(handler.DeviceList);
        }

        // Check if all moles have been intercepted or time has run out
        if (molesRemaining <= 0)
        {
            logger.Log("All mole secured. Iteration won!");
            moleSpeedSlowdown = Math.Max(200, moleSpeedSlowdown - 200);  // Decrease mole slowdown time (speed up)
            updateScore(Score + 100 * lifeLine);
            IterationWon();
            return;
        }

        // Continue receiving input
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    private async void GenerateMoles(CancellationToken cancellationToken)
    {
        int initialMolesCount = targetPerPlayer*config.MaxPlayers;
        try
        {
            while (!cancellationToken.IsCancellationRequested && isGameRunning)
            {
                molePositions.Clear();
                SendColorToDevices(backgroundColor, true);
                for (int i = 0; i < initialMolesCount; i++)
                {
                    GenerateMole();
                }

                // Send updated bullet positions to devices
                handler.SendColorsToUdp(handler.DeviceList);

                await Task.Delay(moleSpeedSlowdown, cancellationToken);
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

    private void GenerateMole()
    {
        int molePos = random.Next(totalTiles);
        while (molePositions.Contains(molePos))
        {
            molePos = random.Next(totalTiles);
        }
        molePositions.Add(molePos);
        if (molesRemaining > (molesperLevel * 2) / 3) handler.DeviceList[molePos] = initialMoleColor;
        else if (molesRemaining > molesperLevel / 3) handler.DeviceList[molePos] = midGameMoleColor;
        else handler.DeviceList[molePos] = endGameMoleColor;
    }

    protected void CancelTargetThread()
    {
        cancellationTokenSource?.Cancel(); // Cancel the running task
        cancellationTokenSource?.Dispose(); // Dispose of the token source
        cancellationTokenSource = null;

    }
}
