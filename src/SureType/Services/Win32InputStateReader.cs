using System.Globalization;
using System.Runtime.InteropServices;
using SureType.Models;
using InputImeMode = SureType.Models.ImeMode;

namespace SureType.Services;

public sealed class Win32InputStateReader : IInputStateReader
{
    private const int VkCapital = 0x14;
    private const int WmImeControl = 0x0283;
    private const int ImcGetConversionMode = 0x0001;
    private const int ImeCmodeNative = 0x0001;
    private const int LangChinesePrimary = 0x04;
    private const int LangEnglishPrimary = 0x09;

    public InputState ReadCurrentState()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == nint.Zero)
        {
            return InputState.Unknown;
        }

        var capsMode = IsCapsLockOn() ? CapsMode.Upper : CapsMode.Lower;
        var threadId = GetWindowThreadProcessId(hwnd, out _);
        var keyboardLayout = GetKeyboardLayout(threadId);
        var languageId = unchecked((ushort)((long)keyboardLayout & 0xffff));
        var primaryLanguageId = languageId & 0x3ff;

        if (primaryLanguageId == LangChinesePrimary)
        {
            return new InputState(InputSource.ChineseIme, ReadImeMode(hwnd), capsMode);
        }

        if (primaryLanguageId == LangEnglishPrimary)
        {
            return new InputState(InputSource.EnglishKeyboard, InputImeMode.NotApplicable, capsMode);
        }

        return new InputState(InputSource.Unknown, InputImeMode.Unknown, capsMode);
    }

    private static InputImeMode ReadImeMode(nint hwnd)
    {
        var imeWindow = ImmGetDefaultIMEWnd(hwnd);
        if (imeWindow == nint.Zero)
        {
            return InputImeMode.Unknown;
        }

        var conversionMode = SendMessage(imeWindow, WmImeControl, ImcGetConversionMode, 0);
        if (conversionMode == nint.Zero)
        {
            return InputImeMode.English;
        }

        return (((long)conversionMode & ImeCmodeNative) == ImeCmodeNative)
            ? InputImeMode.Chinese
            : InputImeMode.English;
    }

    private static bool IsCapsLockOn()
    {
        return (GetKeyState(VkCapital) & 0x0001) != 0;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("imm32.dll")]
    private static extern nint ImmGetDefaultIMEWnd(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
}
