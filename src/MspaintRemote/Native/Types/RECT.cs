using System.Runtime.InteropServices;

namespace MspaintRemote.Native.Types;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}