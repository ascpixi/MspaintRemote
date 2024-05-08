using System.Runtime.InteropServices;
using MspaintRemote.Native.Types;

namespace MspaintRemote.Native;

internal static unsafe partial class Kernel32
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint hObject);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint OpenProcess(
        ProcessAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwProcessId
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(
        nint hProcess,
        nuint lpBaseAddress,
        void* lpBuffer,
        nint nSize,
        [Optional] nint* lpNumberOfBytesRead
    );

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(
        nint hProcess,
        nuint lpBaseAddress,
        void* lpBuffer,
        nint nSize,
        [Optional] nint* lpNumberOfBytesWritten
    );
    
    [LibraryImport("kernel32.dll")]
    public static partial nuint VirtualQueryEx(
        nint hProcess,
        nuint lpAddress,
        out MemoryBasicInformation lpBuffer,
        int dwLength
    );
}