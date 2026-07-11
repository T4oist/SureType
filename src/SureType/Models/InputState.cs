namespace SureType.Models;

public enum InputSource
{
    ChineseIme,
    EnglishKeyboard,
    Unknown
}

public enum ImeMode
{
    Chinese,
    English,
    NotApplicable,
    Unknown
}

public enum CapsMode
{
    Lower,
    Upper
}

public sealed record InputState(InputSource InputSource, ImeMode ImeMode, CapsMode CapsMode)
{
    public static InputState Unknown { get; } = new(InputSource.Unknown, ImeMode.Unknown, CapsMode.Lower);
}
