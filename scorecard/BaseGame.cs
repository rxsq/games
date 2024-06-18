using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

public abstract class BaseGame
{
    public int Score { get; protected set; }
    public TimeSpan Duration { get; protected set; }
    public int Level { get; protected set; }
    public double Progress { get; protected set; }

    protected Timer gameTimer;
    protected Timer durationTimer;
    private string logFile = "c:\\games\\logs\\samsh.log";
    public BaseGame(TimeSpan duration)
    {
        Score = 0;
        Level = 1;
        Progress = 0.0;
        Duration = duration;

        gameTimer = new Timer();
        gameTimer.Interval = 1000; // Check progress every second
        gameTimer.Elapsed += OnGameTick;

        durationTimer = new Timer(Duration.TotalMilliseconds);
        durationTimer.Elapsed += OnDurationElapsed;
    }

    public void StartGame()
    {
        OnStart();
        gameTimer.Start();
        durationTimer.Start();
    }

    public void EndGame()
    {
        gameTimer.Stop();
        durationTimer.Stop();
        OnEnd();
    }

    protected virtual void OnStart() { }
    protected virtual void OnEnd() { }
    protected virtual void OnGameTick(object sender, ElapsedEventArgs e)
    {
        // Update progress based on the elapsed time
        Progress += 1.0 / Duration.TotalSeconds;
        if (Progress >= 1.0)
        {
            EndGame();
        }
    }

    protected virtual void OnDurationElapsed(object sender, ElapsedEventArgs e)
    {
        EndGame();
    }
    public void LogData(string message)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        File.AppendAllText(logFile, logMessage + Environment.NewLine);
        Console.WriteLine(logMessage);
    }
}
