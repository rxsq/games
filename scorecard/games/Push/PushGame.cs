using scorecard.lib;
using scorecard;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using log4net.DateFormatter;

public class PushGame : BaseSingleDevice
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

    public PushGame(GameConfig gameConfig) : base(gameConfig)
    {
        totalTiles = handler.DeviceList.Count;
        columns = config.columns;
        borderTiles = getBorderIndex();
        centerTiles = getCenterTiles();   // Mark the center tiles first
        exampleTiles = GetRemainingLeftTiles();  // Get left tiles excluding center and border
        playTiles = GetRemainingRightTiles();    // Get right tiles excluding center and border
        exampleTileColors = new Dictionary<int, string>();  // Track colors for the target pattern tiles
        playTileColors = new Dictionary<int, string>();  // Track colors for the play tiles
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
        SendColorToDevices(availableColors[0], false); // Set all tiles to blue at the start
        ChnageColorToDevice(borderColor, borderTiles, handler);  // Set borders to white
        ChnageColorToDevice(borderColor, centerTiles, handler);  // Set center tiles to white

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

        // Handle correct hits
        foreach (int tileIndex in positions)
        {
            if (playTiles.Contains(tileIndex))
            {
                // Cycle the color for the touched tile
                CycleTileColor(tileIndex);
            }
        }

        // Send the updated color information to the devices
        handler.SendColorsToUdp(handler.DeviceList);

        if (CombinationMatch())
        {
            logger.Log("Pattern match, display time ended, iteration won.");
            IterationWon(); // Mark the iteration as won when all targets are hit
        }

        // Continue receiving input if the iteration is not complete
        handler.BeginReceive(data => ReceiveCallback(data, handler));
    }



    // Method to cycle the color of a tile
    private void CycleTileColor(int tileIndex)
    {
        // Get the current color index of the tile
        int currentIndex = Array.FindIndex(availableColors, color => color == playTileColors[tileIndex]);

        // Move to the next color in the array (cyclic)
        int nextIndex = (currentIndex + 1) % availableColors.Length;

        // Update the tile's color to the next one in the cycle
        handler.DeviceList[tileIndex] = availableColors[nextIndex];

        // Update the color index for the tile
        playTileColors[tileIndex] = availableColors[nextIndex];
    }

    // Helper function to display target colors and patterns
    protected void DisplayTargetAndPlayPattern()
    {
        for (int i = 0; i < exampleTiles.Count; i++)
        {
            // Ensure each target tile gets a color from the target combination
            if (exampleTileColors.ContainsKey(exampleTiles[i]))
            {
                handler.DeviceList[exampleTiles[i]] = exampleTileColors[exampleTiles[i]];  // Set target tile to its assigned color
            }
            // Ensure each target tile gets a color from the target combination
            if (playTileColors.ContainsKey(playTiles[i]))
            {
                handler.DeviceList[playTiles[i]] = playTileColors[playTiles[i]];  // Set target tile to its assigned color
            }
        }
        handler.SendColorsToUdp(handler.DeviceList);  // Send updated colors to the devices
    }

    // Generate random target combinations for exampleTiles
    protected void GenerateTargetCombination()
    {
        exampleTileColors.Clear();  // Clear previous target colors
        playTileColors.Clear();     // Clear previous play colors

        // Determine the number of columns in each section
        int sectionColumns;
        if (columns % 2 == 0)  // Even number of columns
        {
            sectionColumns = (columns - 4) / 2;
        }
        else  // Odd number of columns
        {
            sectionColumns = (columns - 3) / 2;
        }

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
                int col = i % sectionColumns;

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
                int col = i % sectionColumns;

                // Assign colors for diagonal stripes
                string color = availableColors[(row + col) % availableColors.Length];
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
        while (changePos.Count < config.MaxPlayers * 3)
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

        // Update the play tiles with the new colors
        handler.SendColorsToUdp(handler.DeviceList);
    }


    // Check of the target tiles and play tiles are same
    protected bool CombinationMatch()
    {
        for (int i = 0; i < exampleTiles.Count; i++)
        {
            if(exampleTileColors[exampleTiles[i]] != playTileColors[playTiles[i]]) return false;
        }
        return true;
    }

    // Method to get center tiles
    protected List<int> getCenterTiles()
    {
        List<int> centerTiles = new List<int>();
        int halfColumns = columns / 2;
        bool isOdd = columns % 2 != 0;

        // For odd number of columns, there is one center column
        if (isOdd)
        {
            for (int i = halfColumns; i < totalTiles; i += columns)
            {
                centerTiles.Add(i);
            }
        }
        else
        {
            // For even number of columns, two center columns
            for (int i = halfColumns - 1; i < totalTiles; i += columns)
            {
                centerTiles.Add(i);     // First center column
                centerTiles.Add(i + 1); // Second center column
            }
        }

        return centerTiles;
    }

    // Method to calculate border tiles
    protected List<int> getBorderIndex()
    {
        List<int> borderIndex = new List<int>();

        // Top row (first row)
        for (int i = 0; i < columns; i++)
        {
            borderIndex.Add(i);
        }

        // Bottom row (last row)
        for (int i = totalTiles - columns; i < totalTiles; i++)
        {
            borderIndex.Add(i);
        }

        // Left and right edges (excluding the corners)
        for (int i = columns; i < totalTiles - columns; i += columns)
        {
            borderIndex.Add(i);               // Left edge
            borderIndex.Add(i + columns - 1); // Right edge
        }

        return borderIndex;
    }

    // Method to calculate left tiles excluding center and border
    protected List<int> GetRemainingLeftTiles()
    {
        List<int> leftTiles = new List<int>();
        int halfColumns = columns / 2;
        bool isOdd = columns % 2 != 0;
        int centerStart = isOdd ? halfColumns : halfColumns - 1;

        // Iterate through each row, excluding the first and last row (border)
        for (int row = 1; row < (totalTiles / columns) - 1; row++)
        {
            // Even row (left to right direction)
            if (row % 2 == 0)
            {
                for (int col = 1; col < centerStart; col++)  // Exclude the center and first column
                {
                    int tileIndex = row * columns + col;
                    if (!centerTiles.Contains(tileIndex))  // Ensure not part of the center
                    {
                        leftTiles.Add(tileIndex);
                    }
                }
            }
            // Odd row (right to left direction)
            else
            {
                for (int col = columns - 2; col > centerStart; col--) // Exclude the center and last column
                {
                    int tileIndex = row * columns + col;
                    if (!centerTiles.Contains(tileIndex))  // Ensure not part of the center
                    {
                        leftTiles.Add(tileIndex);
                    }
                }
            }
        }

        return leftTiles;
    }

    // Method to calculate right tiles excluding center and border
    protected List<int> GetRemainingRightTiles()
    {
        List<int> rightTiles = new List<int>();
        int halfColumns = columns / 2;
        bool isOdd = columns % 2 != 0;
        int centerStart = isOdd ? halfColumns + 1 : halfColumns;

        // Iterate through each row, excluding the first and last row (border)
        for (int row = 1; row < (totalTiles / columns) - 1; row++)
        {
            // Even row (left to right direction)
            if (row % 2 == 0)
            {
                for (int col = centerStart + 1; col < columns - 1; col++)  // Exclude the center and last column
                {
                    int tileIndex = row * columns + col;
                    if (!centerTiles.Contains(tileIndex))  // Ensure not part of the center
                    {
                        rightTiles.Add(tileIndex);
                    }
                }
            }
            // Odd row (right to left direction)
            else
            {
                for (int col = centerStart - 1; col > 0; col--) // Exclude the center and first column
                {
                    int tileIndex = row * columns + col;
                    if (!centerTiles.Contains(tileIndex))  // Ensure not part of the center
                    {
                        rightTiles.Add(tileIndex);
                    }
                }
            }
        }

        return rightTiles;
    }
}
