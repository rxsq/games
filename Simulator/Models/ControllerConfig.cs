using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Models
{
    public class ControllerConfig
    {
        public string Name { get; set; }
        public List<ButtonConfig> Buttons { get; set; } = new List<ButtonConfig>();
    }
}
