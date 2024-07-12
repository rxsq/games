
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        //var instance = new MusicPlayer();
        //instance.PlayBackgroundMusic("content/background_music.wav", true);
        //Thread.Sleep(10000);
        //for (int i = 0; i < 100; i++)
        //{
        //    instance.backgroundMusicPlayer.Volume = 0.1f;
        //    Thread.Sleep(2000);
        //    instance.PlayEffect("content/levelwin.wav");
        //    Thread.Sleep(2000);
        //}
        Application.Run(new MainForm());
        }
    }

