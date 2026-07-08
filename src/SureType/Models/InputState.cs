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
    public string DisplayAsset => InputStateAssets.GetAssetKey(this);

    public string DisplayName => (InputSource, ImeMode, CapsMode) switch
    {
        (InputSource.ChineseIme, ImeMode.Chinese, _) => "Chinese input",
        (InputSource.ChineseIme, ImeMode.English, CapsMode.Upper) => "Chinese IME English uppercase",
        (InputSource.ChineseIme, ImeMode.English, _) => "Chinese IME English",
        (InputSource.EnglishKeyboard, _, CapsMode.Upper) => "English uppercase",
        (InputSource.EnglishKeyboard, _, _) => "English lowercase",
        _ => "Unknown input state"
    };

    public static InputState Unknown { get; } = new(InputSource.Unknown, ImeMode.Unknown, CapsMode.Lower);
}

public static class InputStateAssets
{
    public const string ChineseInput = "StatusAsset.ChineseInput";
    public const string ChineseImeEnglish = "StatusAsset.ChineseImeEnglish";
    public const string EnglishLower = "StatusAsset.EnglishLower";
    public const string EnglishUpper = "StatusAsset.EnglishUpper";
    public const string Unknown = "StatusAsset.Unknown";

    public static string GetAssetKey(InputState state)
    {
        return (state.InputSource, state.ImeMode, state.CapsMode) switch
        {
            (InputSource.ChineseIme, ImeMode.Chinese, _) => ChineseInput,
            (InputSource.ChineseIme, ImeMode.English, _) => ChineseImeEnglish,
            (InputSource.EnglishKeyboard, _, CapsMode.Upper) => EnglishUpper,
            (InputSource.EnglishKeyboard, _, _) => EnglishLower,
            _ => Unknown
        };
    }
}
