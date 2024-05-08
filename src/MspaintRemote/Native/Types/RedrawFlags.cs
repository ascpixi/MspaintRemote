namespace MspaintRemote.Native.Types;

[Flags]
internal enum RedrawFlags : uint
{
    Invalidate = 0x00000001,
    InternalPaint = 0x00000002,
    Erase = 0x00000004,
    Validate = 0x00000008,
    NoInternalPaint = 0x00000010,
    NoErase = 0x00000020,
    NoChildren = 0x00000040,
    AllChildren = 0x00000080,
    UpdateNow = 0x00000100,
    EraseNow = 0x00000200,
    Frame = 0x00000400,
    NoFrame = 0x00000800
}