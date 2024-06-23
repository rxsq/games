using System;
using System.Windows.Forms;

class Program
{
    static void Main(string[] args)
    {
       // UdpRelayServer udpRelayServer = new UdpRelayServer(41235, "127.0.0.1", 41236); // Listen on port 41235 and send to port 41236
      //  udpRelayServer.Start();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());

      //  udpRelayServer.Stop();
    }
}
