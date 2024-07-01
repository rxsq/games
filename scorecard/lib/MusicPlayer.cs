using NAudio.Wave;
using System;
using System.IO;

public class MusicPlayer
{
    private WaveOutEvent backgroundMusicPlayer;
    private WaveOutEvent effectsPlayer;
    private AudioFileReader backgroundAudioFile;
    private bool repeatBackgroundMusic;

    public void PlayBackgroundMusic(string filePath, bool repeat = false)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
            return;
        }

        try
        {
            repeatBackgroundMusic = repeat;

            if (backgroundMusicPlayer == null)
            {
                backgroundMusicPlayer = new WaveOutEvent();
                backgroundMusicPlayer.PlaybackStopped += BackgroundMusicPlayer_PlaybackStopped;
            }
            else if (backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
            {
                backgroundMusicPlayer.Stop();
                backgroundMusicPlayer.Dispose();
                backgroundMusicPlayer = new WaveOutEvent();
                backgroundMusicPlayer.PlaybackStopped += BackgroundMusicPlayer_PlaybackStopped;
            }

            backgroundAudioFile = new AudioFileReader(filePath);
            backgroundMusicPlayer.Init(backgroundAudioFile);
            backgroundMusicPlayer.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing background music: {ex.Message}");
        }
    }

    private void BackgroundMusicPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
    {
        if (repeatBackgroundMusic && backgroundAudioFile != null)
        {
            backgroundAudioFile.Position = 0;
            backgroundMusicPlayer.Play();
        }
    }

    public void PlayEffect(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
            return;
        }

        try
        {
            if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
            {
                effectsPlayer.Stop();
                effectsPlayer.Dispose();
            }

            effectsPlayer = new WaveOutEvent();
            var audioFile = new AudioFileReader(filePath);
            effectsPlayer.Init(audioFile);
            effectsPlayer.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing effect: {ex.Message}");
        }
    }

    public void StopBackgroundMusic()
    {
        if (backgroundMusicPlayer != null && backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
        {
            repeatBackgroundMusic = false;
            backgroundMusicPlayer.Stop();
            backgroundMusicPlayer.Dispose();
            backgroundMusicPlayer = null;
        }
    }

    public void StopAllMusic()
    {
        StopBackgroundMusic();

        if (effectsPlayer != null && effectsPlayer.PlaybackState == PlaybackState.Playing)
        {
            effectsPlayer.Stop();
            effectsPlayer.Dispose();
            effectsPlayer = null;
        }
    }
}
