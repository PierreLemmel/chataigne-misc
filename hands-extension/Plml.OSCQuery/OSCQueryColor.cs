using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vizcon.OSC;

namespace Plml.OSCQuery;

public static class OSCQueryColor
{
    public static string RGBAToString(RGBA color) => $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    public static RGBA StringToRGBA(string colorString)
    {
        if (!IsOSCQueryColorString(colorString))
            throw new ArgumentException("Invalid color string length.");

        int r = Convert.ToInt32(colorString.Substring(0, 2), 16);
        int g = Convert.ToInt32(colorString.Substring(2, 2), 16);
        int b = Convert.ToInt32(colorString.Substring(4, 2), 16);
        int a = Convert.ToInt32(colorString.Substring(6, 2), 16);

        return new RGBA((byte)r, (byte)g, (byte)b, (byte)a);
    }

    public static bool IsOSCQueryColorString(string oscString) => Regex.IsMatch(oscString, @"^[0-9A-Fa-f]{8}$");
}
