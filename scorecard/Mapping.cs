using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scorecard
{
    public class Mapping
    {
        public UdpHandler udpHandler;
        public bool isActive;
        public int deviceNo;

        public Mapping(UdpHandler udpHandler, bool isActive, int deviceNo)
        {
            this.udpHandler = udpHandler;
            this.isActive = isActive;
            this.deviceNo = deviceNo;
        }
        private List<int> dg;
        public List<int> DeviceGroup
        {
            get
            {
                if (dg == null) { new List<int>(); }
                return dg;
            }
            set
            {
                dg = value;
            }
        }
    }
}

