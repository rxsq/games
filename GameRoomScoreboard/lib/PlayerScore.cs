using System;


namespace scorecard
{
    public class PlayerScore
    {

        public int? PlayerID { get; set; }

        public int WristbandTranID { get; set; }

        public string Src { get; set; }

        public string WristbandCode { get; set; }

        public int Score { get; set; }
        public int Level { get; set; }

        public DateTimeOffset? PlayerStartTime { get; set; }

        public DateTimeOffset? PlayerEndTime { get; set; }

        public string GameType { get; set; }
    }
}
