using log4net.Core;
using scorecard.lib;
using scorecard;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Timers;

public class HexaPatternMatch : BaseSingleDevice
{
    private double targetPercentage;
    private int targetCount;
    private string gameColor;
    private Task patternTask;
    private List<int> targetTiles = new List<int>(); // Holds target tiles
    private HashSet<int> hitTargets = new HashSet<int>(); // Track hit targets
    private HashSet<int> hitTiles = new HashSet<int>(); // Track hit tiles
    private int wrongAttempts = 0; // Counter for wrong attempts
    private const int maxWrongAttempts = 3; // Max number of wrong hits allowed
    private bool displayTimeEnded = false; // Track if display time has ended
    private const int maxTargetCount = 25; // Maximum number of targets for higher levels
    private Timer intervalTimer; // Timer to show lights at intervals
    private const int intervalTime = 3000;

    public HexaPatternMatch(GameConfig config) : base(config)
    {
    }

    protected override void Initialize()
    {
        targetCount = config.MaxPlayers * 2; // Initial target count based on players
        base.BlinkAllAsync(2); // Blink at the start of the game
    }

    protected override async void StartAnimition()
    {
        base.StartAnimition();
    }

    // Logic for each iteration
    protected override void OnIteration()
    {
        SendColorToDevices(ColorPalette.Blue, false); // Set all tiles to blue at the start
        wrongAttempts = 0; // Reset wrong attempts at the start of each iteration
        displayTimeEnded = false; // Reset display time flag for the new iteration
        hitTargets.Clear(); // Clear previously hit tiles
        hitTiles.Clear(); // Clear previously hit tiles
        CalculateTargetCountForCurrentLevel(); // Dynamically calculate the number of targets based on the current level
        ActivateRandomLights(); // Activate target lights and set a timer for hiding
        intervalTimer = new Timer(intervalTime);
        intervalTimer.Elapsed += (sender, e) => DisplayRemainingTargets(); // Show lights at regular intervals
    }

    // Dynamically calculate the number of targets based on the current level
    private void CalculateTargetCountForCurrentLevel()
    {
        // Adjust the target count to increase gradually as the level increases
        // Using a formula that increases targets linearly but at a slower rate.
        // Cap it at maxTargetCount (25).
        targetCount = Math.Min(config.MaxPlayers * 2 + (int)(Level * 0.5), maxTargetCount);

        // Log the calculated target count for the current level
        logger.Log($"Level {Level}: Setting target count to {targetCount}");
    }

    // Start receiving data when the game starts
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    // Activate random lights as targets and hide them after a delay
    private void ActivateRandomLights()
    {
        targetTiles.Clear();
        handler.activeDevices.Clear();

        // Set all tiles to blue first, ensuring no stray yellow lights appear
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.DeviceList[i] = ColorPalette.Blue; // Set all devices to blue initially
        }

        int attempts = 0; // Counter to ensure we don't get stuck in an infinite loop
        int maxAttempts = 100; // Max attempts to find random tiles

        // Ensure that we have the correct number of target tiles (targetCount)
        while (targetTiles.Count < targetCount && attempts < maxAttempts)
        {
            int index = random.Next(handler.DeviceList.Count());

            // Only set this tile as a target if it is not already in the targetTiles list
            if (!targetTiles.Contains(index))
            {
                handler.DeviceList[index] = ColorPalette.yellow; // Set target tile to yellow
                targetTiles.Add(index); // Add this tile to the target list
                handler.activeDevices.Add(index); // Make this tile clickable
            }

            attempts++; // Increment attempts to avoid infinite loop
        }

        // If we were unable to select enough targets within the attempt limit, log an error
        if (targetTiles.Count < targetCount)
        {
            logger.Log($"ERROR: Unable to activate the correct number of target tiles. Activated {targetTiles.Count}/{targetCount}.");
        }

        // Send the updated colors for all devices
        handler.SendColorsToUdp(handler.DeviceList);

        LogData($"Target tiles activated: {string.Join(",", targetTiles)}");

        // Set a timer to hide targets after the display time ends
        Task.Delay(CalculateDisplayTimeForLevel()).ContinueWith(_ => HideTargets());
    }

    // Calculate how long the targets will be displayed
    private int CalculateDisplayTimeForLevel()
    {
        // Decrease the display time as the level increases (e.g., 2 seconds base, -200ms per level)
        int displayTime = Math.Max(500, 2000 - (Level - 1) * 200); // Ensure the display time doesn't go below 500ms
        logger.Log($"Displaying targets for {displayTime} milliseconds at level {Level}");
        return displayTime;
    }

    // Hide target tiles after the display period ends, but don't reset hit tiles
    private void HideTargets()
    {
        foreach (var index in targetTiles)
        {
            if (!hitTargets.Contains(index)) // Only hide tiles that were not hit
            {
                handler.DeviceList[index] = ColorPalette.Blue; // Set hidden targets back to blue
            }
        }
        handler.SendColorsToUdp(handler.DeviceList); // Update tiles
        displayTimeEnded = true; // Mark the display phase as ended
        logger.Log("Hiding targets and allowing iteration progression");
    }

    private void DisplayRemainingTargets()
    {
        foreach (var index in targetTiles)
        {
            if (!hitTargets.Contains(index)) // Only hide tiles that were not hit
            {
                handler.DeviceList[index] = ColorPalette.yellow; // Set hidden targets back to yellow
            }
        }
        // Send the updated colors for all devices
        handler.SendColorsToUdp(handler.DeviceList);

        // Set a timer to hide targets after the display time ends
        Task.Delay(CalculateDisplayTimeForLevel()).ContinueWith(_ => HideTargets());
    }

    // Callback to handle touch inputs
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning) // Ensure the game is running
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        // Get touched device positions
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        // Process correct hits
        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Green : ColorPalette.Green, touchedActiveDevices, handler);
            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            positions.RemoveAll(x => touchedActiveDevices.Contains(x));
            hitTargets.UnionWith(touchedActiveDevices); // Track hit tiles
            hitTiles.UnionWith(touchedActiveDevices); // Track hit tiles
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
        }

        positions.RemoveAll(x => hitTargets.Contains(x) || hitTiles.Contains(x));
        // Process wrong hits
        if (positions.Count > 0)
        {
            hitTiles.UnionWith(positions);
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Red : ColorPalette.Red, positions, handler);
            wrongAttempts++; // Increment wrong attempts
            logger.Log($"Wrong tile hit! Wrong attempts: {wrongAttempts}");

            // Check if max wrong attempts are reached
            if (wrongAttempts >= maxWrongAttempts)
            {
                logger.Log("Max wrong attempts reached, iteration lost.");
                IterationLost(null); // End iteration due to too many wrong hits
                return;
            }

            positions.Clear();
        }

        // Move to the next iteration when all targets are hit and display time has ended
        if (handler.activeDevices.Count == 0 && displayTimeEnded)
        {
            logger.Log("All target tiles hit, display time ended, iteration won.");
            IterationWon(); // Mark the iteration as won when all targets are hit
        }
        else
        {
            // Continue receiving inputs if not all target tiles are hit
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
}
