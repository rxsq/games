using NAudio.Wave;

public class MusicPlayer
{
    private WaveOutEvent backgroundMusicPlayer;
    private WaveOutEvent scoreMusicPlayer;
    private WaveOutEvent winMusicPlayer;

    public void PlayBackgroundMusic(string filePath)
    {
        backgroundMusicPlayer = new WaveOutEvent();
        var audioFile = new AudioFileReader(filePath);
        backgroundMusicPlayer.Init(audioFile);
        backgroundMusicPlayer.Play();
    }

    public void PlayScoreMusic(string filePath)
    {
        scoreMusicPlayer = new WaveOutEvent();
        var audioFile = new AudioFileReader(filePath);
        scoreMusicPlayer.Init(audioFile);
        scoreMusicPlayer.Play();
    }

    public void PlayWinMusic(string filePath)
    {
        winMusicPlayer = new WaveOutEvent();
        var audioFile = new AudioFileReader(filePath);
        winMusicPlayer.Init(audioFile);
        winMusicPlayer.Play();
    }
}
