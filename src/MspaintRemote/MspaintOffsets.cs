namespace MspaintRemote;

/// <summary>
/// Contains offsets from the base of the mspaint.exe binary to well-known memory locations.
/// </summary>
public static class MspaintOffsets
{
    public const int CanvasWidth = 0xDE738;
    public const int CanvasHeight = 0xDE72C;
    public const int PrimaryBrush = 0xE0E64;
    public const int SecondaryBrush = 0xDD644;
    public const int Tool = 0xD9068;
}