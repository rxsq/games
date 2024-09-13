using log4net.Core;
using scorecard.lib;
using scorecard;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System;

public class HexaPatternMatch : BaseSingleDevice
{
    private double targetPercentage;
    private int targetCount;
    private string gameColor;
    private Task patternTask;
    private List<int> targetTiles = new List<int>(); // Holds target tiles
    private int wrongAttempts = 0; // Counter for wrong attempts
    private const int maxWrongAttempts = 3; // Max number of wrong hits allowed
                                            //   private bool displayTimeEnded = false; // Track if display time has ended

    public HexaPatternMatch(GameConfig config) : base(config)
    {
    }

    protected override void Initialize()
    {
        targetCount = config.MaxPlayers * 2; // Calculate initial target count
        base.BlinkAllAsync(2); // Blink at the start of the game
    }

    protected override async void StartAnimition()
    {
        base.StartAnimition();
    }

    // Logic for each iteration
    protected override void OnIteration()
    {
        gameColor = gameColors[random.Next(gameColors.Count - 1)]; // Random color for target tiles
        SendColorToDevices(ColorPalette.Blue, false);
        //SetAllTilesToBlue(); // Ensure all tiles are blue at the start of the iteration
        wrongAttempts = 0; // Reset wrong attempts at the start of each iteration
                           //  displayTimeEnded = false; // Reset display time flag for the new iteration
        CalculateTargetCountForCurrentLevel(); // Dynamically calculate the number of targets based on the current level
        ActivateRandomLights(); // Activate target lights
       // patternTask = Task.Run(() => DisplayAndHideTargets()); // Display targets and hide after a delay
    }

    // Dynamically calculate the number of targets based on the current level
    private void CalculateTargetCountForCurrentLevel()
    {
        targetCount = config.MaxPlayers * 2 + (Level - 1); // Increase target count as level increases
        logger.Log($"Level {Level}: Setting target count to {targetCount}");
    }

    // Start receiving data when the game starts
    protected override void OnStart()
    {
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }

    // Activate random lights as targets
    private void ActivateRandomLights()
    {
        targetTiles.Clear();
        handler.activeDevices.Clear();

        // Set all tiles to blue first
        //SetAllTilesToBlue();
        SendColorToDevices(ColorPalette.Blue, false);

        // Activate a percentage of random lights as targets
        while (targetTiles.Count < targetCount)
        {
            int index = random.Next(handler.DeviceList.Count());
            if (!targetTiles.Contains(index))
            {
                handler.DeviceList[index] = ColorPalette.yellow; // Set target tile to yellow
                targetTiles.Add(index);
                handler.activeDevices.Add(index); // Make target tiles clickable
            }
        }

        // Send the updated colors to the device
        handler.SendColorsToUdp(handler.DeviceList);
        LogData($"Target tiles activated: {string.Join(",", targetTiles)}");
        // Decrease the display time as the level increases (e.g., 2 seconds base, -200ms per level)
        int displayTime = Math.Max(500, 2000 - (Level - 1) * 200); // Ensure the display time doesn't go below 500ms

        logger.Log($"Displaying targets in yellow for {displayTime} milliseconds at level {Level}");

        Task.Delay(displayTime); // Display target tiles for the calculated time

        // Hide target tiles and set them back to blue
        SendColorToDevices(ColorPalette.Blue, false);
        //  HideTargets();
        // displayTimeEnded = true; // Mark the display phase as ended
        logger.Log("Hiding targets and allowing iteration progression");
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

        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));
        if (touchedActiveDevices.Count > 0)
        {
            if (!isGameRunning)
                return;
            ChnageColorToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Green : ColorPalette.Green, touchedActiveDevices, handler);
            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
            positions.RemoveAll(x => touchedActiveDevices.Contains(x));
            updateScore(Score + 1);
            LogData($"Score updated: {Score}  position {String.Join(",", positions)} active positions:{string.Join(",", handler.activeDevices)}");
        }
        //process wrong tagets
        if (positions.Count > 0)
        {
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

        }
        

        // Move to the next iteration when all targets are hit AND the display time has ended
        if (handler.activeDevices.Count == 0)//&& displayTimeEnded)
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




