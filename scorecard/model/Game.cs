using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.model
{
    public class Game
    {
        public int GameID { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string gameDescription { get; set; }
        public int MaxPlayers { get; set; }
        public string IpAddress { get; set; }
        public int LocalPort { get; set; }
        public int RemotePort { get; set; }
        public int SocketBReceiverPort { get; set; }
        public int NoOfControllers { get; set; }
        public int NoofLedPerdevice { get; set; }
        public int columns { get; set; }
        public string SmartPlugip { get; set; }
    }

    public class GameVariant
    {
        public int ID { get; set; }
        public string name { get; set; }
        public string variantDescription { get; set; }
        public string Levels { get; set; }
        public string BackgroundImage { get; set; }
        public string iconImage { get; set; }
        public string video { get; set; }
        public string instructions { get; set; }
        public int MaxIterations { get; set; }
        public int MaxIterationTime { get; set; }
        public int MaxLevel { get; set; }
        public int ReductionTimeEachLevel { get; set; }
        public int GameId { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public Game game { get; set; }
        public string introAudio { get; set; }
    }

}
