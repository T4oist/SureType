using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using SureType.Models;
using SureType.Services;
using SureType.Windows;

internal static class NativeChecks
{
    public static int Run()
    {
        using var ready = new ManualResetEventSlim();
        nint handle = 0;
        Dispatcher? dispatcher = null;
        var thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            using var source = new HwndSource(new HwndSourceParameters("SureType timeout test") { Width = 1, Height = 1 });
            source.AddHook((nint h, int m, nint w, nint l, ref bool handled) =>
            {
                if (m == 0x0283) { handled = true; if (w == 5) Thread.Sleep(400); return 0; }
                return 0;
            });
            handle = source.Handle; ready.Set(); Dispatcher.Run();
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!ready.Wait(5000)) throw new Exception("Native test window did not start");
        try
        {
            if (!Win32InputStateReader.Query(handle, 1, out var result) || result != 0)
                throw new Exception("Successful zero IME result was not distinguished from failure");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (Win32InputStateReader.Query(handle, 5, out _)) throw new Exception("Unresponsive window was not timed out");
            if (watch.ElapsedMilliseconds > 300) throw new Exception("IME timeout exceeded expected bound");
            if (Win32InputStateReader.Query(new nint(-12345), 1, out _)) throw new Exception("Invalid window unexpectedly succeeded");
            Console.WriteLine("PASS native message success with zero result, invalid window, and 100ms timeout");
        }
        finally { dispatcher!.BeginInvokeShutdown(DispatcherPriority.Send); thread.Join(2000); }
        var app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var settings = new AppSettings { OverlayDurationSeconds = 0.5 };
        var overlay = new OverlayWindow(settings);
        var foreground = GetForegroundWindow();
        foreach (var position in Enum.GetValues<OverlayPosition>())
        {
            settings.OverlayPosition = position;
            overlay.ShowState(new InputState(InputSource.ChineseIme, ImeMode.English, CapsMode.Upper));
            Pump(100);
            if (GetForegroundWindow() != foreground) throw new Exception("Overlay stole foreground focus");
            var hwnd = new WindowInteropHelper(overlay).Handle;
            if (SendMessage(hwnd, 0x0084, 0, 0) != -1) throw new Exception("Overlay is not hit-test transparent");
            if (!GetWindowRect(hwnd, out var rect)) throw new Exception("Overlay has no bounds");
            var area = System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
            if (rect.Left < area.Left || rect.Top < area.Top || rect.Right > area.Right || rect.Bottom > area.Bottom)
                throw new Exception("Overlay exceeds screen work area");
            overlay.HideImmediately();
            if (overlay.IsVisible) throw new Exception("Immediate hide failed");
        }
        overlay.ShowState(InputState.Unknown); Pump(550);
        overlay.ShowState(InputState.Unknown); Pump(260);
        if (!overlay.IsVisible) throw new Exception("Old fade completion hid a new cue");
        Pump(700);
        if (overlay.IsVisible) throw new Exception("Cue did not expire");
        overlay.Close(); app.Shutdown();
        Console.WriteLine("PASS five overlay positions, no focus activation, click-through, immediate hide, and fade replacement");
        return 0;
    }
    private static void Pump(int ms)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint hwnd, uint msg, nint w, nint l);
}
