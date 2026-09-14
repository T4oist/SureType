namespace SureType.Models;

public sealed record StatusPresentation(string Symbol, bool CapsLock)
{
    public static StatusPresentation FromState(InputState state) => new(
        (state.InputSource, state.ImeMode) switch
        {
            (InputSource.ChineseIme, ImeMode.Chinese) => "中",
            (InputSource.ChineseIme, ImeMode.English) => "英",
            (InputSource.EnglishKeyboard, _) => "EN",
            _ => "?"
        }, state.CapsMode == CapsMode.Upper);
}
