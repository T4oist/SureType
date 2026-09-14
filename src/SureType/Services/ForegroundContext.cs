using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace SureType.Services;

public sealed record ForegroundContext(nint Handle, string? Executable, bool IsFullscreen, bool IsSelf)
{
    public static ForegroundContext Read()
    {
        var hwnd = GetForegroundWindow();
        GetWindowThreadProcessId(hwnd, out var pid);
        string? executable = null;
        using (var process = OpenProcess(0x1000, false, pid))
        {
            if (!process.IsInvalid)
            {
                var buffer = new StringBuilder(32768);
                uint length = (uint)buffer.Capacity;
                if (QueryFullProcessImageName(process, 0, buffer, ref length)) executable = buffer.ToString();
            }
        }
        var bounds = Forms.Screen.FromHandle(hwnd).Bounds;
        var fullscreen = hwnd != 0 && GetWindowRect(hwnd, out var rect) &&
            rect.Left <= bounds.Left && rect.Top <= bounds.Top && rect.Right >= bounds.Right && rect.Bottom >= bounds.Bottom;
        return new(hwnd, executable, fullscreen, pid == Environment.ProcessId);
    }
    public bool IsSuppressed(bool fullscreen, string[] excluded) =>
        Handle == 0 || IsSelf || (fullscreen && IsFullscreen) ||
        (Executable != null && excluded.Contains(Executable, StringComparer.OrdinalIgnoreCase));
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder buffer, ref uint size);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
}
