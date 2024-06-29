using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.lib
{
    public class Device
    {
        public string  color { get; set; }
        public int sequence { get; set; }

        public bool isActive { get; set; } = false;
    }
}
