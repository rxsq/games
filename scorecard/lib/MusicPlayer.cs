using NAudio.Wave;
using System;
using System.IO;

public class MusicPlayer
{
    private WaveOutEvent backgroundMusicPlayer;
    private WaveOutEvent efeectsPlayer;

    public void PlayBackgroundMusic(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
            return;
        }

        try
        {
            if (backgroundMusicPlayer == null)
            {
                backgroundMusicPlayer = new WaveOutEvent();
            }
            else if (backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
            {
                backgroundMusicPlayer.Stop();
                backgroundMusicPlayer.Dispose();
                backgroundMusicPlayer = new WaveOutEvent();
            }

            var audioFile = new AudioFileReader(filePath);
            backgroundMusicPlayer.Init(audioFile);
            backgroundMusicPlayer.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing background music: {ex.Message}");
        }
    }

    public void PlayEfeect(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Music File not found: {filePath}");
            return;
        }

        try
        {
            if (efeectsPlayer != null && efeectsPlayer.PlaybackState == PlaybackState.Playing)
            {
                efeectsPlayer.Stop();
                efeectsPlayer.Dispose();
            }

            efeectsPlayer = new WaveOutEvent();
            var audioFile = new AudioFileReader(filePath);
            efeectsPlayer.Init(audioFile);
            efeectsPlayer.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing efeect: {ex.Message}");
        }
    }

    public void StopBackgroundMusic()
    {
        if (backgroundMusicPlayer != null && backgroundMusicPlayer.PlaybackState == PlaybackState.Playing)
        {
            backgroundMusicPlayer.Stop();
            backgroundMusicPlayer.Dispose();
            backgroundMusicPlayer = null;
        }
    }

    public void StopAllMusic()
    {
        StopBackgroundMusic();

        if (efeectsPlayer != null && efeectsPlayer.PlaybackState == PlaybackState.Playing)
        {
            efeectsPlayer.Stop();
            efeectsPlayer.Dispose();
            efeectsPlayer = null;
        }
    }
}
