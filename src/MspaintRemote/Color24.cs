using System.Drawing;
using System.Runtime.InteropServices;

namespace MspaintRemote;

/// <summary>
/// Represents a 24-bit RGB color, internally represented in BGR order.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Color24(byte r, byte g, byte b)
{
    public byte B = b;
    public byte G = g;
    public byte R = r;

    public static implicit operator Color24(Color x) => new(x.R, x.G, x.B);

    public static implicit operator Color(Color24 x) => Color.FromArgb(255, x.R, x.G, x.B);
}