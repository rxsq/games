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

    protected int getHighestScoreIndex()
    {
        int highestScore = scores[0];
        int highestScoreIndex = 0;

        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > highestScore)
            {
                highestScore = scores[i];
                highestScoreIndex = i;
            }
        }
        return highestScoreIndex;
    }

    override protected void IterationWon()
    {
        isGameRunning = false;
        udpHandlers.ForEach(x => x.StopReceive());
        LogData($"All targets hit iterations:{iterations} passed");
        if (config.timerPointLoss)
            iterationTimer.Dispose();
        iterations = iterations + 1;



        if (iterations >= config.Maxiterations)
        {

            Status = $"{GameStatus.Running}: Moved to Next Level {Level}";
            LogData($"Game Win level: {Level}");
            Level = Level + 1;
            iterations = 1;
            int highest = getHighestScoreIndex();
            if (Level >= config.MaxLevel)
            {
                Status = $"Reached to last Level {config.MaxLevel} ending game. Player {highest+1} wins";
                LogData(Status);
                musicPlayer.Announcement($"content/voicelines/winPlayer{highest+1}.mp3");
                EndGame();
                return;
            }
            else
            {
                //Text to speech: Great job, Team! 🎉You’ve won this level! Now, get ready for the next one.Expect more energy and excitement.  Let’s go! 🚀 one two three go 
                LogData(Status);
                musicPlayer.Announcement($"content/voicelines/level_{Level}.mp3");
            }

        }
        else { BlinkAllAsync(1); }
        Status = $"{GameStatus.Running}: Moved to Next iterations {iterations}";
        RunGameInSequence();
        LogData($"moving to next iterations: {iterations} Iteration time: {IterationTime} ");
    }
}
