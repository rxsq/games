using System.Collections.Generic;
using System.Drawing;

public static class ColorMapper
{
    public static readonly Dictionary<string, Color> ColorMap = new Dictionary<string, Color>
    {
        { "ff0000", Color.FromArgb(0xff, 0x00, 0x00) },  // Red
        { "00ff00", Color.FromArgb(0x00, 0xff, 0x00) },  // Green
        { "0000ff", Color.FromArgb(0x00, 0x00, 0xff) },  // Blue
        { "ffff00", Color.FromArgb(0xff, 0xff, 0x00) },  // Yellow
        { "000000", Color.FromArgb(0x00, 0x00, 0x00) },  // NoColor
        { "ffc0cb", Color.FromArgb(0xff, 0xc0, 0xcb) },  // Pink
        { "00ffff", Color.FromArgb(0x00, 0xff, 0xff) },  // Cyan
        { "ff00ff", Color.FromArgb(0xff, 0x00, 0xff) },  // Magenta
        { "ffa500", Color.FromArgb(0xff, 0xa5, 0x00) },  // Orange
        { "800080", Color.FromArgb(0x80, 0x00, 0x80) },  // Purple
        { "bfff00", Color.FromArgb(0xbf, 0xef, 0x00) },  // Lime
        { "008080", Color.FromArgb(0x00, 0x80, 0x80) },  // Teal
        { "e6e6fa", Color.FromArgb(0xe6, 0xe6, 0xfa) },  // Lavender
        { "a52a2a", Color.FromArgb(0xa5, 0x2a, 0x2a) },  // Brown
        { "800000", Color.FromArgb(0x80, 0x00, 0x00) },  // Maroon
        { "000080", Color.FromArgb(0x00, 0x00, 0x80) },  // Navy
        { "808000", Color.FromArgb(0x80, 0x80, 0x00) },  // Olive
        { "ff7f50", Color.FromArgb(0xff, 0x7f, 0x50) },  // Coral
        { "ffd700", Color.FromArgb(0xff, 0xd7, 0x00) },  // Gold
        { "c0c0c0", Color.FromArgb(0xc0, 0xc0, 0xc0) },  // Silver
        { "808080", Color.FromArgb(0x80, 0x80, 0x80) },  // Gray
        { "ffffff", Color.FromArgb(0xff, 0xff, 0xff) }   // White
    };
}