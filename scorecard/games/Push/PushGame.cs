using scorecard.lib;
using scorecard;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using log4net.DateFormatter;
using System.Runtime.InteropServices;

public class PushGame : BaseMultiDevice
{
    private string[] availableColors = { ColorPaletteone.Blue, ColorPaletteone.Orange, ColorPaletteone.Green, ColorPaletteone.Red, ColorPaletteone.Yellow, ColorPaletteone.Purple };
    private string borderColor = ColorPaletteone.White;
    private List<int> borderTiles;
    private List<int> exampleTiles;  // These represent the target pattern tiles
    private List<int> playTiles;
    private List<int> centerTiles;
    private Dictionary<int, string> exampleTileColors; // Track the colors for the example (target) tiles
    private Dictionary<int, string> playTileColors; // Track the colors for the play tiles
    private int totalTiles;
    private int columns;
    private int targetPerPlayer = 3;
    CoolDown coolDown;

    public PushGame(GameConfig gameConfig) : base(gameConfig)
    {
        totalTiles = deviceMapping.Count;
        rows = config.columns;
        columns = totalTiles/config.columns;
        borderTiles = GetBorderIndex();
        centerTiles = GetCenterTiles();   // Mark the center tiles first
        exampleTiles = GetRemainingLeftTiles();  // Get left tiles excluding center and border
        playTiles = GetRemainingRightTiles();    // Get right tiles excluding center and border
        exampleTileColors = new Dictionary<int, string>();  // Track colors for the target pattern tiles
        playTileColors = new Dictionary<int, string>();  // Track colors for the play tiles
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
        coolDown.SetFlagTrue(200);
        SendColorToDevices(availableColors[0], false); // Set all tiles to blue at the start
        foreach(int bt in borderTiles)
        {
            deviceMapping[bt].udpHandler.DeviceList[deviceMapping[bt].deviceNo] = borderColor;
        }
        foreach (int bt in centerTiles)
        {
            deviceMapping[bt].udpHandler.DeviceList[deviceMapping[bt].deviceNo] = borderColor;
        }
        foreach (var handler in udpHandlers)
        {
            handler.SendColorsToUdp(handler.DeviceList);
        }
        //foreach (var handler in udpHandlers)
        //{
        //    ChnageColorToDevice(borderColor, borderTiles, handler);  // Set borders to white
        //    ChnageColorToDevice(borderColor, centerTiles, handler);  // Set center tiles to white
        //}

        // Populate and display the target pattern (exampleTiles)
        GenerateTargetCombination();

        DisplayTargetAndPlayPattern();
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning) return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);

        // Parse the touched device positions
        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => (x.index - 2) / config.NoofLedPerdevice)
                                          .ToList();

        if(positions.Count > 0 && !coolDown.Flag)
        {
            
            // Handle correct hits
            foreach (int tileIndex in positions)
            {
                int actualPos = GetKeyFromDeviceMapping(handler, tileIndex);
                if (playTiles.Contains(actualPos))
                {
                    // Cycle the color for the touched tile
                    CycleTileColor(actualPos, tileIndex, handler);
                }
            }

            // Send the updated color information to the devices
            handler.SendColorsToUdp(handler.DeviceList);

            if (CombinationMatch())
            {
                logger.Log("Pattern match, display time ended, iteration won.");
                updateScore(Score + config.MaxPlayers * targetPerPlayer + Level * LifeLine);
                IterationWon(); // Mark the iteration as won when all targets are hit
            }
        }

        // Continue receiving input if the iteration is not complete
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }



    // Method to cycle the color of a tile
    private void CycleTileColor(int actualPos, int tileIndex, UdpHandler handler)
    {
        // Get the current color index of the tile
        int currentIndex = Array.FindIndex(availableColors, color => color == playTileColors[actualPos]);

        // Move to the next color in the array (cyclic)
        int nextIndex = (currentIndex + 1) % availableColors.Length;

        // Update the tile's color to the next one in the cycle
        handler.DeviceList[tileIndex] = availableColors[nextIndex];

        // Update the color index for the tile
        playTileColors[actualPos] = availableColors[nextIndex];
    }

    // Helper function to display target colors and patterns
    protected void DisplayTargetAndPlayPattern()
    {
        for(int i = 0; i < exampleTiles.Count; i++)
        {
            Mapping exampleMapping = deviceMapping[exampleTiles[i]];
            Mapping playMapping = deviceMapping[playTiles[i]];
            exampleMapping.udpHandler.DeviceList[exampleMapping.deviceNo] = exampleTileColors[exampleTiles[i]];
            playMapping.udpHandler.DeviceList[playMapping.deviceNo] = playTileColors[playTiles[i]];
        }

        foreach(var handler in udpHandlers)
        {
            //for (int i = 0; i < exampleTiles.Count; i++)
            //{
            //    // Ensure each target tile gets a color from the target combination
            //    if (exampleTileColors.ContainsKey(exampleTiles[i]))
            //    {
            //        handler.DeviceList[deviceMapping[exampleTiles[i]].deviceNo] = exampleTileColors[exampleTiles[i]];  // Set target tile to its assigned color
            //    }
            //    // Ensure each target tile gets a color from the target combination
            //    if (playTileColors.ContainsKey(playTiles[i]))
            //    {
            //        handler.DeviceList[deviceMapping[playTiles[i]].deviceNo] = playTileColors[playTiles[i]];  // Set target tile to its assigned color
            //    }
            //}
            handler.SendColorsToUdp(handler.DeviceList);  // Send updated colors to the devices
        }
    }

    // Generate random target combinations for exampleTiles
    protected void GenerateTargetCombination()
    {
        exampleTileColors.Clear();  // Clear previous target colors
        playTileColors.Clear();     // Clear previous play colors

        // Determine the number of columns in each section
        int sectionColumns = rows - 2;

        if (Level == 1)
        {
            // Uniform color pattern for level 1
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                string color = availableColors[1]; // Use a predefined color for simplicity
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }
        else if (Level == 2)
        {
            // Level 2: Vertical stripes without snake counting
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                int col = i / sectionColumns;

                // Assign the same color to each column in the sections
                string color = availableColors[col % availableColors.Length];
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }
        else if (Level == 3)
        {
            // Level 3: Checkerboard pattern without snake counting
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                int row = i / sectionColumns;
                int col = i % sectionColumns;

                // Assign alternating colors for the checkerboard pattern
                string color = ((row + col) % 2 == 0) ? availableColors[0] : availableColors[1];
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }
        else if (Level == 4)
        {
            // Level 4: Concentric shapes without snake counting
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                int row = i / sectionColumns;
                int col = i % sectionColumns;

                // Calculate the layer (distance from the edge)
                int layer = Math.Min(Math.Min(row, sectionColumns - 1 - col), Math.Min(col, totalTiles / sectionColumns - 1 - row));

                // Assign colors based on layer within sections
                string color = availableColors[layer % availableColors.Length];
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }
        else if (Level == 5)
        {
            // Level 5: Diagonal stripes without snake counting
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                int row = i / sectionColumns;
                int col = row%2==0?i % sectionColumns:sectionColumns - i%sectionColumns - 1;
                int colIndex = (row + col) % availableColors.Count();
                // Assign colors for diagonal stripes
                string color = availableColors[colIndex];
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }
        else
        {
            // Level 6 and above: Random pattern
            for (int i = 0; i < exampleTiles.Count; i++)
            {
                int colorIndex = random.Next(availableColors.Length);
                string color = availableColors[colorIndex];
                exampleTileColors[exampleTiles[i]] = color;
                playTileColors[playTiles[i]] = color;
            }
        }

        ChangePlayTileColors();
    }

    // Change The colors of play tiles to allow players to solve
    protected void ChangePlayTileColors()
    {
        List<int> changePos = new List<int>();
        int playTileLength = playTiles.Count;
        int colorLength = availableColors.Length;

        // Ensure that the number of changed positions is less than or equal to MaxPlayers * 3
        while (changePos.Count < config.MaxPlayers * targetPerPlayer)
        {
            int randomPos = random.Next(playTileLength);

            // Ensure the position hasn't been chosen already
            if (!changePos.Contains(randomPos))
            {
                changePos.Add(randomPos);

                // Generate a random color that is different from the example tile's color
                string randomColor = availableColors[random.Next(colorLength)];
                while (randomColor == exampleTileColors[exampleTiles[randomPos]])
                {
                    randomColor = availableColors[random.Next(colorLength)];
                }

                // Now assign the final random color to the play tile
                playTileColors[playTiles[randomPos]] = randomColor;
            }
        }

        //foreach(var handler in udpHandlers)
        //{
        //    // Update the play tiles with the new colors
        //    handler.SendColorsToUdp(handler.DeviceList);
        //}
    }


    // Check of the target tiles and play tiles are same
    protected bool CombinationMatch()
    {
        coolDown.SetFlagTrue(100);
        for (int i = 0; i < exampleTiles.Count; i++)
        {
            if(exampleTileColors[exampleTiles[i]] != playTileColors[playTiles[i]]) return false;
        }
        return true;
    }

    // Method to get center tiles
    protected List<int> GetCenterTiles()
    {
        List<int> centerTiles = new List<int>();
        int halfColumns = (columns / 2)*rows;
        bool isOdd = columns % 2 != 0;

        // For odd number of columns, there is one center column
        if (isOdd)
        {
            for (int i = 1; i < rows-1; i++)
            {
                centerTiles.Add(halfColumns+i);
            }
        }
        else
        {
            // For even number of columns, two center columns
            for (int i = 1; i < rows - 1; i++)
            {
                centerTiles.Add(halfColumns + i);
                centerTiles.Add(halfColumns - i - 1); 
            }
        }

        return centerTiles;
    }

    // Method to calculate border tiles
    protected List<int> GetBorderIndex()
    {
        List<int> borderIndex = new List<int>();

        // (first column)
        for (int i = 0; i < rows; i++)
        {
            borderIndex.Add(i);
        }

        // (last column)
        for (int i = totalTiles - rows; i < totalTiles; i++)
        {
            borderIndex.Add(i);
        }

        // top and bottom row (excluding the corners)
        for (int i = rows; i < totalTiles - rows; i += rows)
        {
            borderIndex.Add(i);               
            borderIndex.Add(i + rows - 1); 
        }

        return borderIndex;
    }

    // Method to calculate left tiles excluding center and border
    protected List<int> GetRemainingLeftTiles()
    {
        List<int> leftTiles = new List<int>();
        int halfColumns = columns / 2 * rows + rows;
        bool isOdd = columns % 2 != 0;

        for(int i = halfColumns; i<totalTiles-rows; i += rows)
        {
            for(int j = 1; j<rows-1; j++)
            {
                leftTiles.Add(i + j);
            }
        }

        return leftTiles;
    }

    // Method to calculate right tiles excluding center and border
    protected List<int> GetRemainingRightTiles()
    {
        List<int> rightTiles = new List<int>();
        
        bool isOdd = columns % 2 != 0;
        int halfColumns = isOdd ? columns / 2 * rows - 1 : columns / 2 * rows - rows - 1;

        for (int i = rows; i <= halfColumns; i += rows)
        {
            for (int j = 1; j < rows - 1; j++)
            {
                rightTiles.Add(i + j);
            }
        }

        return rightTiles;
    }
}
