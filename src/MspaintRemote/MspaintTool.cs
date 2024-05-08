namespace MspaintRemote;

public enum MspaintTool : uint
{
    Brush = 0xA59DEE10,
    Line = 0xA59DF130,
    Text = 0xA59E0250,
    Fill = 0xA59DEBB0,
    Pencil = 0xA59DEF50,
    Eraser = 0xA59DEAB0,
    ColorPicker = 0xA59DEC00,
    Magnifier = 0xA59DEC60,
    Select = 0xA59DEA50,
    FreeformSelect = 0xA59E0170,
    // TODO: there's loads more
}