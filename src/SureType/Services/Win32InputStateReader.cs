using System.Runtime.InteropServices;
using SureType.Models;
using InputImeMode = SureType.Models.ImeMode;

namespace SureType.Services;

public sealed class Win32InputStateReader : IInputStateReader
{
    public InputState ReadCurrentState()
    {
        var hwnd = GetForegroundWindow();
        var caps = (GetKeyState(0x14) & 1) != 0 ? CapsMode.Upper : CapsMode.Lower;
        if (hwnd == 0) return new(InputSource.Unknown, InputImeMode.Unknown, caps);
        var thread = GetWindowThreadProcessId(hwnd, out _);
        if (thread == 0) return new(InputSource.Unknown, InputImeMode.Unknown, caps);
        var language = (long)GetKeyboardLayout(thread) & 0x3ff;
        if (language == 9) return new(InputSource.EnglishKeyboard, InputImeMode.NotApplicable, caps);
        if (language != 4) return new(InputSource.Unknown, InputImeMode.Unknown, caps);
        var info = new GuiThreadInfo { Size = Marshal.SizeOf<GuiThreadInfo>() };
        if (GetGUIThreadInfo(thread, ref info) && info.Focus != 0) hwnd = info.Focus;
        var ime = ImmGetDefaultIMEWnd(hwnd);
        if (ime == 0 || !Query(ime, 5, out var open))
            return new(InputSource.ChineseIme, InputImeMode.Unknown, caps);
        if (open == 0) return new(InputSource.ChineseIme, InputImeMode.English, caps);
        var mode = Query(ime, 1, out var conversion)
            ? ((conversion & 1) != 0 ? InputImeMode.Chinese : InputImeMode.English)
            : InputImeMode.Unknown;
        return new(InputSource.ChineseIme, mode, caps);
    }
    internal static bool Query(nint hwnd, nint command, out nuint result) =>
        SendMessageTimeout(hwnd, 0x0283, command, 0, 0x0002 | 0x0020, 100, out result) != 0;
    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public int Size, Flags;
        public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public int Left, Top, Right, Bottom;
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern nint GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("imm32.dll")] private static extern nint ImmGetDefaultIMEWnd(nint hwnd);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern nint SendMessageTimeout(nint hwnd, uint message, nint wParam, nint lParam, uint flags, uint timeout, out nuint result);
}
