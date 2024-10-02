using System;
using System.Collections.Generic;
using scorecard.lib;
public abstract class BaseMultiplayerGame:BaseGame
{
    protected HashSet<int> activeIndicesSingle;
    public UdpHandler handler;
    protected List<string> devices;
    private int[] scores;
    override public string Status
    {
        get { return status; }
        set
        {
            status = value;
            statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnStatusChanged(status);
        }
    }
    override public int Level
    {
        get { return level; }
        set
        {
            level = value;
            statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnLevelChanged(level);
        }
    }
    public int[] Scores
    {
        get { return scores; }
        set
        {
            value.CopyTo(scores,0);
            statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnScoresChanged(scores);
            LogData($"Scores: {string.Join(", ", scores)}");
        }
    }
    override public int LifeLine
    {
        get { return lifeLine; }
        set
        {
            lifeLine = value; statusPublisher.PublishStatus(scores, lifeLine, Level, status, IterationTime, config.GameName, iterations);
            OnLifelineChanged(value);
            LogData($"LifeLine: {lifeLine}");
        }
    }
    public event EventHandler<int[]> ScoresChanged;
    protected virtual void OnScoresChanged(int[] newScore)
    {
        LogData($"score changed to: {string.Join(", ", scores)}");
        ScoresChanged?.Invoke(this, newScore);
    }
    public BaseMultiplayerGame(GameConfig co):base(co)
    {
        scores = new int[co.MaxPlayers];
        statusPublisher.PublishStatus(scores, config.MaxLifeLines, Level, GameStatus.NotStarted, IterationTime, config.GameName, iterations);
        handler = udpHandlers[0];
    }  
    protected void updateScore(int newScore, int position)
    {
        Scores[position] = newScore;
        int random = new Random().Next(0, 9);
        if (0 <= random && random < 3) { musicPlayer.PlayEffect("content//hit2.wav"); }
        if (3 <= random && random < 6) { musicPlayer.PlayEffect("content/hit2.wav"); }
        if (6 <= random) { musicPlayer.PlayEffect("content/hit2.wav"); }
    }
}
