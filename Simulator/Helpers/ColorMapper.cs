using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator.Helpers
{
    internal class ColorMapper
    {
        public static readonly Dictionary<string, KnownColor> ColorMap = new Dictionary<string, KnownColor>
        {
            { "ff0000", KnownColor.Red },       // Red
            { "00ff00", KnownColor.Lime },      // Green
            { "0000ff", KnownColor.Blue },      // Blue
            { "ffff00", KnownColor.Yellow },    // Yellow
            { "000000", KnownColor.Black },     // NoColor (Black)
            { "ffc0cb", KnownColor.Pink },      // Pink
            { "00ffff", KnownColor.Cyan },      // Cyan
            { "ff00ff", KnownColor.Magenta },   // Magenta
            { "ffa500", KnownColor.Orange },    // Orange
            { "800080", KnownColor.Purple },    // Purple
            { "bfff00", KnownColor.YellowGreen }, // Lime (YellowGreen is the closest known color)
            { "008080", KnownColor.Teal },      // Teal
            { "e6e6fa", KnownColor.Lavender },  // Lavender
            { "a52a2a", KnownColor.Brown },     // Brown
            { "800000", KnownColor.Maroon },    // Maroon
            { "000080", KnownColor.Navy },      // Navy
            { "808000", KnownColor.Olive },     // Olive
            { "ff7f50", KnownColor.Coral },     // Coral
            { "ffd700", KnownColor.Gold },      // Gold
            { "c0c0c0", KnownColor.Silver },    // Silver
            { "808080", KnownColor.Gray },      // Gray
            { "ffffff", KnownColor.White }      // White
        };
    }
}
