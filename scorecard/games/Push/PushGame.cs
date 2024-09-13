using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class PushGame : BaseGame
{
    private const int MaxButtons = 300; // Total number of push buttons
    private Dictionary<int, string> exampleGrid; // Example grid for Spot the Difference
    private Dictionary<int, string> sharedGrid; // Shared grid for all players
    private List<int> correctButtons; // List of buttons that have been correctly set
    private bool isGameCompleted = false;
    private readonly string[] availableColors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };

    public PushGame(GameConfig config) : base(config)
    {
        exampleGrid = GenerateExampleGrid(); // Generate the target grid
        sharedGrid = InitializeSharedGrid(); // Initialize a shared grid for all players
        correctButtons = new List<int>(); // Track correct button presses
    }

    protected override void Initialize()
    {
        base.Initialize();
        BlinkAllAsync(3); // Blink LEDs as part of game start animation
    }

    protected override void OnStart()
    {
        // Start the game by lighting up both the example grid and the shared grid
        DisplayExampleGrid();
        DisplaySharedGrid();
    }

    // Generate an example grid for players to match
    private Dictionary<int, string> GenerateExampleGrid()
    {
        var grid = new Dictionary<int, string>();
        Random random = new Random();

        for (int i = 0; i < MaxButtons; i++)
        {
            grid.Add(i, availableColors[random.Next(availableColors.Length)]); // Randomly assign colors to the example grid
        }

        return grid;
    }

    // Initialize the shared grid with blank or random colors
    private Dictionary<int, string> InitializeSharedGrid()
    {
        var grid = new Dictionary<int, string>();

        for (int i = 0; i < MaxButtons; i++)
        {
            grid.Add(i, "NoColor"); // Start with blank (NoColor)
        }

        return grid;
    }

    // Display the example grid for players to compare against
    private void DisplayExampleGrid()
    {
        foreach (var handler in udpHandlers)
        {
            var colors = new List<string>();

            foreach (var item in exampleGrid)
            {
                colors.Add(item.Value); // Show example grid colors
            }

            handler.SendColorsToUdp(colors);
        }
    }

    // Display the shared grid (initially blank or random colors)
    private void DisplaySharedGrid()
    {
        foreach (var handler in udpHandlers)
        {
            var colors = new List<string>();

            foreach (var item in sharedGrid)
            {
                colors.Add(item.Value); // Show the initial colors for the shared grid
            }

            handler.SendColorsToUdp(colors);
        }
    }

    // Receive button presses and update the shared grid
    protected override void OnIteration()
    {
        base.OnIteration();

        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ProcessButtonPress(data, handler));
        }
    }

    // Process button press and update the shared grid accordingly
    private void ProcessButtonPress(byte[] receivedBytes, UdpHandler handler)
    {
        if (isGameCompleted) return; // Ignore if game is already completed

        string receivedData = System.Text.Encoding.UTF8.GetString(receivedBytes);
        int buttonIndex = Convert.ToInt32(receivedData); // Assuming the button index is sent in the received data

        // Cycle the color for the pressed button
        sharedGrid[buttonIndex] = GetNextColor(sharedGrid[buttonIndex]);

        // Send updated colors to the UDP handler
        var updatedColors = sharedGrid.Values.ToList();
        handler.SendColorsToUdp(updatedColors);

        // Check if the shared grid matches the example grid
        if (CheckGridCompletion())
        {
            HandleCollectiveWin(); // Announce the group's win
        }
    }

    // Get the next color in the sequence for a button
    private string GetNextColor(string currentColor)
    {
        int currentIndex = Array.IndexOf(availableColors, currentColor);
        return availableColors[(currentIndex + 1) % availableColors.Length]; // Cycle through the color options
    }

    // Check if the shared grid matches the example grid
    private bool CheckGridCompletion()
    {
        foreach (var button in sharedGrid)
        {
            if (button.Value != exampleGrid[button.Key])
            {
                return false; // If any button is incorrect, return false
            }
        }
        return true; // All buttons are correct
    }

    // Announce the collective win and end the game
    private void HandleCollectiveWin()
    {
        isGameCompleted = true; // Mark the game as completed
        musicPlayer.Announcement("Congratulations! You've completed the puzzle!");
        EndGame();
    }
}
