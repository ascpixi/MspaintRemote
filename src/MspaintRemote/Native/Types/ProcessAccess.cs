namespace MspaintRemote.Native.Types;

[Flags]
internal enum ProcessAccess : uint
{
    CreateProcess = 0x0080,
    CreateThread = 0x0002,
    DuplicateHandle = 0x0040,
    QueryInformation = 0x0400,
    QueryLimitedInformation = 0x1000,
    SetInformation = 0x0200,
    SetQuota = 0x0100,
    SuspendResume = 0x0800,
    Terminate = 0x0001,
    VmOperation = 0x0008,
    VmRead = 0x0010,
    VmWrite = 0x0020,
    Synchronize = 0x00100000
}