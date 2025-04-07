using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace scorecard
{
    public class BaseSingleDevice: BaseGame
    {
        protected HashSet<int> activeIndicesSingle;
        public UdpHandler handler;
        protected List<string> devices;

        protected int rows = 0;
        public BaseSingleDevice(GameConfig config) : base(config)
        {
           
            handler = udpHandlers[0];
           
        }
        public BaseSingleDevice(GameConfig config, string backgroundMusic) : base(config, backgroundMusic)
        {

            handler = udpHandlers[0];

        }
    }   
}
