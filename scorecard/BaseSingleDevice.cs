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
        protected UdpHandler handler;
        protected List<string> devices;
        public BaseSingleDevice(GameConfig config) : base(config)
        {
            handler = udpHandlers[0];
            devices = handlerDevices[handler];
            activeIndicesSingle= activeIndices[handler];
        }

      

       
    }
   
}
