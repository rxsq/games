using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.lib
{
    public class GameConfig
    {
        public int Maxiterations { get; set; } = 5;
        public int MaxIterationTime { get; set; } = 30; // Assuming time is in seconds
        public int MaxLevel { get; set; } = 10;
        public int ReductionTimeEachLevel { get; set; } = 5; // Assuming time is in seconds
        public int MaxPlayers { get; set; } = 5;
        public string IpAddress { get; internal set; } = "192.168.0.7";
        public int LocalPort { get; internal set; } = 21;
        public int RemotePort { get; internal set; } = 7113;
        public int SocketBReceiverPort { get; internal set; } = 20105;
        public int NoOfControllers { get; internal set; } = 1;
        public int NoofLedPerdevice { get; internal set; } = 1;
        public int columns { get; set; } = 14;
        public string introAudio { get; set; } = "";

        public bool timerPointLoss = true;
        public string SmartPlugip {  get; set; }

    }
}
