using System.Runtime.InteropServices;
using MspaintRemote.Native.Types;

namespace MspaintRemote.Native;

internal static unsafe partial class User32
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RedrawWindow(
        nint hWnd,
        [Optional] RECT* lprcUpdate,
        nint hrgnUpdate,
        RedrawFlags flags
    );

    [LibraryImport("user32.dll", EntryPoint = "PostMessageA")]
    public static partial uint PostMessage(
        nint hWnd,
        uint msg,
        nuint wParam,
        nint lParam
    );

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
    public static partial uint MapVirtualKey(
        VirtualKey uCode,
        uint uMapType
    );
}