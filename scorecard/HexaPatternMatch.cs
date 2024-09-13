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
    private bool displayTimeEnded = false; // Track if display time has ended

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
        SetAllTilesToBlue(); // Ensure all tiles are blue at the start of the iteration
        wrongAttempts = 0; // Reset wrong attempts at the start of each iteration
        displayTimeEnded = false; // Reset display time flag for the new iteration
        CalculateTargetCountForCurrentLevel(); // Dynamically calculate the number of targets based on the current level
        ActivateRandomLights(); // Activate target lights
        patternTask = Task.Run(() => DisplayAndHideTargets()); // Display targets and hide after a delay
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
        SetAllTilesToBlue();

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
    }

    // Display targets and hide them after a set time
    private async Task DisplayAndHideTargets()
    {
        // Decrease the display time as the level increases (e.g., 2 seconds base, -200ms per level)
        int displayTime = Math.Max(500, 2000 - (Level - 1) * 200); // Ensure the display time doesn't go below 500ms

        logger.Log($"Displaying targets in yellow for {displayTime} milliseconds at level {Level}");

        await Task.Delay(displayTime); // Display target tiles for the calculated time

        // Hide target tiles and set them back to blue
        HideTargets();
        displayTimeEnded = true; // Mark the display phase as ended
        logger.Log("Hiding targets and allowing iteration progression");
    }

    // Hide target tiles by turning them blue
    private void HideTargets()
    {
        foreach (var index in targetTiles)
        {
            handler.DeviceList[index] = ColorPalette.Blue; // Set hidden targets back to blue
        }
        handler.SendColorsToUdp(handler.DeviceList); // Update tiles
    }

    // Ensure all non-target tiles are blue
    private void SetAllTilesToBlue()
    {
        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            handler.DeviceList[i] = ColorPalette.Blue; // Set all tiles to blue
        }
        handler.SendColorsToUdp(handler.DeviceList);
        logger.Log("All tiles set to blue");
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

        // Process all touched positions (both correct and wrong)
        foreach (var position in positions)
        {
            if (targetTiles.Contains(position)) // Correct tile
            {
                handler.DeviceList[position] = ColorPalette.Green; // Turn correct tiles green
                targetTiles.Remove(position); // Remove clicked tile from target list
                updateScore(Score + 1); // Increase score for correct hit
                logger.Log($"Correct tile hit! Score updated: {Score}");
            }
            else // Wrong tile
            {
                handler.DeviceList[position] = ColorPalette.Red; // Turn wrong tiles red
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
        }

        // Send updated colors to the device
        handler.SendColorsToUdp(handler.DeviceList);

        // Move to the next iteration when all targets are hit AND the display time has ended
        if (targetTiles.Count == 0 && displayTimeEnded)
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




//using scorecard;
//using scorecard.lib;

//public class HexaPatternMatch : BaseSingleDevice
//{
//    private List<int> patternIndices = new List<int>();  // Holds the pattern indices
//    private int currentRound = 1;
//    private int currentIteration = 1;
//    private int score = 0;
//    private const int maxRounds = 10;
//    private int iterationsPerLevel = 3; // Fixed iterations per level
//    private int targetTilesCount = 3;   // Initial number of target tiles for level 1
//    private Task patternTask;
//    private int timeLimitPerRound = 5000; // Initial time limit per round in milliseconds

//    public HexaPatternMatch(GameConfig config) : base(config)
//    {
//        // Initialize game properties
//    }

//    protected override void Initialize()
//    {
//        var handler = udpHandlers[0];

//        if (handler == null)
//        {
//            throw new NullReferenceException("Handler is null. Ensure UdpHandler is properly initialized.");
//        }

//        SetAllTilesToBlue();
//        BlinkAllAsync(2); // Blink all devices to signal game start
//    }

//    protected override void OnStart()
//    {
//        if (patternTask == null || patternTask.IsCompleted)
//        {
//            if (patternTask != null && !patternTask.IsCompleted)
//            {
//                logger.Log("Pattern task still running");
//            }
//            logger.Log("Starting pattern task");
//            patternTask = Task.Run(() => DisplayPattern());
//        }

//        udpHandlers[0].BeginReceive(data => ReceiveCallback(data, udpHandlers[0]));
//    }

//    protected async override void OnIteration()
//    {
//        logger.Log($"Starting new iteration {currentIteration} of level {currentRound}");

//        // Ensure all tiles are blue before setting a new pattern
//        SetAllTilesToBlue();

//        // Clear previous activeDevices and patternIndices
//        logger.Log("Clearing activeDevices and patternIndices before setting new pattern");
//        patternIndices.Clear();
//        udpHandlers[0].activeDevices.Clear();

//        // Set the number of targets based on the current level
//        targetTilesCount = currentRound + 2; // Increase the number of targets with the level

//        // Set the new pattern for the current iteration
//        SetPattern();

//        // Start blinking to indicate the start of the iteration
//        BlinkAllAsync(1);

//        // Display the pattern temporarily and then hide it, making the tiles clickable
//        logger.Log("Starting pattern task");
//        patternTask = Task.Run(async () =>
//        {
//            await DisplayPattern();

//            // Make sure activeDevices is updated after hiding the pattern to make them clickable
//            udpHandlers[0].activeDevices = new List<int>(patternIndices);
//            logger.Log($"Pattern set and ready for clicks: {string.Join(",", udpHandlers[0].activeDevices)}");

//            // Start receiving input again after hiding the pattern
//            udpHandlers[0].BeginReceive(data => ReceiveCallback(data, udpHandlers[0]));
//        });

//        // Log the active devices after the pattern is set
//        logger.Log($"New pattern set with active devices: {string.Join(",", udpHandlers[0].activeDevices)}");
//    }


//    private async Task DisplayPattern()
//    {
//        if (!isGameRunning)
//            return;

//        // Display the pattern in yellow for a brief period
//        logger.Log("Displaying pattern in yellow");
//        SetPatternColor(ColorPalette.yellow);
//        await Task.Delay(2000);

//        // Hide the pattern and set the tiles back to blue
//        logger.Log("Hiding pattern and setting tiles to blue");
//        SetPatternColor(ColorPalette.Blue);
//    }

//    private void SetPatternColor(string color)
//    {
//        lock (udpHandlers)
//        {
//            foreach (int index in patternIndices)
//            {
//                udpHandlers[0].DeviceList[index] = color; // Set the color for pattern tiles
//            }

//            udpHandlers[0].SendColorsToUdp(udpHandlers[0].DeviceList); // Send updated colors to all tiles
//            logger.Log($"Setting pattern tiles to color {color} for indices: {string.Join(",", patternIndices)}");
//        }
//    }

//    private void SetAllTilesToBlue()
//    {
//        lock (udpHandlers)
//        {
//            for (int i = 0; i < udpHandlers[0].DeviceList.Count(); i++)
//            {
//                udpHandlers[0].DeviceList[i] = ColorPalette.Blue; // Set all devices to blue
//            }
//            udpHandlers[0].SendColorsToUdp(udpHandlers[0].DeviceList); // Send blue color to all tiles
//            logger.Log("Set all tiles to blue");
//        }
//    }

//    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
//    {
//        if (!isGameRunning)
//            return;

//        string receivedData = Encoding.UTF8.GetString(receivedBytes);

//        // Get touched device positions
//        List<int> positions = receivedData.Select((value, index) => new { value, index })
//                                          .Where(x => x.value == 0x0A)
//                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
//                                          .Where(position => position >= 0)
//                                          .ToList();

//        // Only process clicks on active devices
//        var touchedActiveDevices = handler.activeDevices.FindAll(x => positions.Contains(x));

//        if (touchedActiveDevices.Count > 0)
//        {
//            foreach (var device in touchedActiveDevices)
//            {
//                if (patternIndices.Contains(device)) // Correct tile
//                {
//                    handler.DeviceList[device] = ColorPalette.Green; // Turn correct tiles green
//                    patternIndices.Remove(device);  // Remove clicked tile from pattern
//                }
//                else
//                {
//                    handler.DeviceList[device] = ColorPalette.Red; // Turn incorrect tiles red
//                }
//            }

//            handler.SendColorsToUdp(handler.DeviceList); // Update tile colors
//            handler.activeDevices.RemoveAll(x => touchedActiveDevices.Contains(x));
//            score++;
//            logger.Log($"Score updated: {score}, touched positions: {String.Join(",", positions)}, active positions: {string.Join(",", handler.activeDevices)}");
//        }

//        // Check if all pattern tiles have been clicked
//        if (patternIndices.Count == 0)
//        {
//            logger.Log("All pattern tiles clicked, moving to next iteration");
//            IterationWon();
//        }
//        else
//        {
//            // Continue receiving inputs if not all pattern tiles are clicked
//            handler.BeginReceive(data => ReceiveCallback(data, handler));
//        }
//    }

//    private void SetPattern()
//    {
//        var handler = udpHandlers[0];
//        string patternColor = ColorPalette.yellow;

//        // Set up a new pattern for the round
//        logger.Log("Setting up new pattern");
//        for (int i = 0; i < targetTilesCount; i++)
//        {
//            int index;
//            do
//            {
//                index = random.Next(0, handler.DeviceList.Count()); // Get a random tile
//            } while (patternIndices.Contains(index)); // Avoid selecting the same tile twice

//            patternIndices.Add(index);  // Add to pattern
//        }

//        // Set pattern color for the active pattern tiles
//        SetPatternColor(patternColor);

//        // Make sure only pattern tiles are clickable after displaying
//        handler.activeDevices = new List<int>(patternIndices);
//        logger.Log($"Pattern set with clickable devices: {string.Join(",", handler.activeDevices)}");
//    }

//    private void IterationWon()
//    {
//        // Increment iteration and check if the level is complete
//        currentIteration++;

//        if (currentIteration > iterationsPerLevel)
//        {
//            // Level complete, move to the next level
//            currentRound++;
//            currentIteration = 1;  // Reset iteration count for the new level

//            // Increase the number of targets for the next level
//            targetTilesCount = currentRound + 2;

//            logger.Log($"Level {currentRound - 1} completed. Moving to level {currentRound}");
//        }

//        if (currentRound > maxRounds)
//        {
//            EndGame();
//        }
//        else
//        {
//            OnIteration(); // Proceed to the next iteration or level
//        }
//    }

//    private void EndGame()
//    {
//        isGameRunning = false;
//        logger.Log($"Game over! Final score: {score}");
//        ResetGame();
//    }

//    private void ResetGame()
//    {
//        currentRound = 1;
//        currentIteration = 1;
//        score = 0;
//        timeLimitPerRound = 5000; // Reset the time limit per round
//        targetTilesCount = 3;     // Reset the number of target tiles
//        patternIndices.Clear();
//        logger.Log("Game reset");
//    }
//}