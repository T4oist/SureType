using System.IO;
using System.Drawing;
using System.Windows.Threading;
using SureType.Models;
using SureType.Services;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--ui") return UiChecks.Interact();
        if (args.Length == 1 && args[0] == "--native") return NativeChecks.Run();
        if (args.Length == 3 && args[0] == "--lifecycle") return LifecycleChecks.Run(args[1], args[2], 0);
        if (args.Length >= 3 && args[0] == "--soak") return LifecycleChecks.Run(args[1], "", double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture));
        if (args.Length == 2 && args[0] == "--render") return UiChecks.Run(args[1]);
        var tests = new (string Name, Action Body)[]
        {
            ("Settings clamp and reject non-finite values", () => {
                var s = new AppSettings { OverlayDurationSeconds = double.NaN, InputFocusCooldownSeconds = double.PositiveInfinity, OverlaySize = 999 };
                Equal(1.5, s.OverlayDurationSeconds); Equal(8d, s.InputFocusCooldownSeconds); Equal(96d, s.OverlaySize);
                s.OverlayDurationSeconds = 10; s.InputFocusCooldownSeconds = -1;
                Equal(5d, s.OverlayDurationSeconds); Equal(1d, s.InputFocusCooldownSeconds);
                s.OverlayPosition = (OverlayPosition)99; s.LogoStyle = (LogoStyle)99; s.Language = (AppLanguage)99;
                Equal(OverlayPosition.TopRight, s.OverlayPosition); Equal(LogoStyle.Filled, s.LogoStyle); Equal(AppLanguage.Chinese, s.Language);
            }),
            ("Settings roundtrip includes mouse position and exclusions", () => InTemporaryDirectory(dir => {
                var store = new SettingsStore(Path.Combine(dir, "settings.json"));
                var s = new AppSettings { OverlayPosition = OverlayPosition.NearMouse, Language = AppLanguage.English,
                    ShowOnInputFocus = false, HasCompletedOnboarding = true, ExcludedApplications = [@"C:\Apps\editor.exe"] };
                True(store.Save(s)); var loaded = store.Load();
                Equal(OverlayPosition.NearMouse, loaded.OverlayPosition); Equal(AppLanguage.English, loaded.Language);
                True(!loaded.ShowOnInputFocus && loaded.HasCompletedOnboarding); Equal(1, loaded.ExcludedApplications.Length);
                loaded.OverlaySize = 80; True(store.Save(loaded)); Equal(80d, store.Load().OverlaySize);
                Equal(0, Directory.GetFiles(dir, "*.tmp").Length);
            })),
            ("Corrupt JSON is preserved before replacement", () => InTemporaryDirectory(dir => {
                var path = Path.Combine(dir, "settings.json"); File.WriteAllText(path, "{broken");
                var store = new SettingsStore(path); var loaded = store.Load();
                True(store.LastError != null); Equal("{broken", File.ReadAllText(path));
                True(store.Save(loaded)); var backups = Directory.GetFiles(dir, "*.corrupt-*");
                Equal(1, backups.Length); Equal("{broken", File.ReadAllText(backups[0]));
            })),
            ("Save failures retain old data and report an error", () => InTemporaryDirectory(dir => {
                var parentFile = Path.Combine(dir, "not-a-directory"); File.WriteAllText(parentFile, "keep");
                var store = new SettingsStore(Path.Combine(parentFile, "settings.json"));
                True(!store.Save(new())); True(store.LastError != null); Equal("keep", File.ReadAllText(parentFile));
            })),
            ("Exclusions normalize and cannot mutate settings by alias", () => {
                var s = new AppSettings { ExcludedApplications = [@"C:\A.exe", @"c:\a.exe", "relative.exe"] };
                Equal(1, s.ExcludedApplications.Length);
                var alias = s.ExcludedApplications; alias[0] = @"C:\B.exe";
                Equal(@"C:\A.exe", s.ExcludedApplications[0]);
            }),
            ("Reset preserves language onboarding and startup", () => {
                var s = new AppSettings { Language = AppLanguage.English, HasCompletedOnboarding = true, StartWithWindows = true, OverlayPosition = OverlayPosition.NearMouse };
                s.ResetPreferences(); Equal(AppLanguage.English, s.Language); True(s.StartWithWindows && s.HasCompletedOnboarding);
                Equal(OverlayPosition.TopRight, s.OverlayPosition);
            }),
            ("CapsLock is independently visible in every input mode", () => {
                foreach (var source in Enum.GetValues<InputSource>())
                foreach (var mode in Enum.GetValues<ImeMode>())
                {
                    var lower = StatusPresentation.FromState(new(source, mode, CapsMode.Lower));
                    var upper = StatusPresentation.FromState(new(source, mode, CapsMode.Upper));
                    Equal(lower.Symbol, upper.Symbol); True(!lower.CapsLock && upper.CapsLock);
                }
                Equal("中", StatusPresentation.FromState(new(InputSource.ChineseIme, ImeMode.Chinese, CapsMode.Lower)).Symbol);
                Equal("英", StatusPresentation.FromState(new(InputSource.ChineseIme, ImeMode.English, CapsMode.Upper)).Symbol);
                Equal("?", StatusPresentation.FromState(InputState.Unknown).Symbol);
            }),
            ("Focus cooldown never suppresses real state changes", () => {
                var p = new CuePolicy();
                True(p.ShouldShow(0, false, true, false, false, false, true, true, 8));
                True(!p.ShouldShow(500, false, true, false, false, false, true, true, 8));
                True(p.ShouldShow(501, true, false, false, false, false, true, true, 8));
                True(p.ShouldShow(8000, false, true, false, false, false, true, true, 8));
            }),
            ("Duplicate focus event after state cue is merged", () => {
                var p = new CuePolicy();
                True(p.ShouldShow(0, true, false, false, false, false, true, true, 8));
                True(!p.ShouldShow(50, false, true, false, false, false, true, true, 8));
                True(p.ShouldShow(151, false, true, false, false, false, true, true, 8));
            }),
            ("Manual previews bypass pause and suppression", () => {
                var p = new CuePolicy();
                True(!p.ShouldShow(0, true, true, false, true, false, true, true, 8));
                True(!p.ShouldShow(0, true, true, false, false, true, true, true, 8));
                True(p.ShouldShow(0, false, false, true, true, true, false, false, 8));
                p.Reset(); True(p.ShouldShow(0, false, true, false, false, false, true, true, 8));
            }),
            ("Disabled triggers do not show", () => {
                True(!new CuePolicy().ShouldShow(0, true, true, false, false, false, false, false, 8));
            }),
            ("Fullscreen self and app path suppression", () => {
                True(new ForegroundContext(1, @"C:\App.exe", false, false).IsSuppressed(false, [@"c:\app.exe"]));
                True(new ForegroundContext(1, null, true, false).IsSuppressed(true, []));
                True(!new ForegroundContext(1, null, true, false).IsSuppressed(false, []));
                True(new ForegroundContext(1, null, false, true).IsSuppressed(false, []));
                True(new ForegroundContext(0, null, false, false).IsSuppressed(false, []));
            }),
            ("Mouse placement flips at right bottom and handles negative monitors", () => {
                Equal(new Point(914, 714), OverlayPlacement.Calculate(new(0, 0, 1000, 800), new(990, 790), 56, 20, OverlayPosition.NearMouse));
                var area = new Rectangle(-1920, -1080, 1920, 1080);
                foreach (var position in Enum.GetValues<OverlayPosition>())
                foreach (var cursor in new[] { new Point(-1920, -1080), new Point(-1, -1), new Point(-800, -600) })
                foreach (var size in new[] { 56, 84, 112 })
                {
                    var p = OverlayPlacement.Calculate(area, cursor, size, 24, position);
                    True(area.Contains(new Rectangle(p, new Size(size, size))));
                }
            }),
            ("Mouse placement stays away from cursor away from edges", () => {
                Equal(new Point(520, 420), OverlayPlacement.Calculate(new(0, 0, 1920, 1040), new(500, 400), 56, 20, OverlayPosition.NearMouse));
            }),
            ("Reader exceptions degrade to unknown", () => {
                var reader = new FakeReader(() => throw new System.ComponentModel.Win32Exception());
                using var service = NewService(reader); var shown = 0;
                service.StateChanged += (_, e) => { Equal(InputState.Unknown, e.State); shown++; };
                service.ShowCurrentState(); PumpUntil(() => shown == 1);
            }),
            ("State change is delivered once across repeated refreshes", () => {
                var state = new InputState(InputSource.EnglishKeyboard, ImeMode.NotApplicable, CapsMode.Lower);
                using var service = NewService(new FakeReader(() => state));
                var shown = 0; var updates = 0;
                service.StateChanged += (_, _) => shown++; service.StateUpdated += (_, _) => updates++;
                service.Request(); PumpUntil(() => updates == 1); Equal(1, shown);
                service.Request(); PumpUntil(() => updates == 2); Equal(1, shown);
                state = state with { CapsMode = CapsMode.Upper };
                service.Request(); PumpUntil(() => updates == 3); Equal(2, shown);
            }),
            ("Pause stops automatic cues but permits preview then resumes", () => {
                var state = new InputState(InputSource.EnglishKeyboard, ImeMode.NotApplicable, CapsMode.Lower);
                using var service = NewService(new FakeReader(() => state)); var shown = 0; var updates = 0;
                service.StateChanged += (_, _) => shown++; service.StateUpdated += (_, _) => updates++;
                service.TogglePaused(); service.Request(); PumpUntil(() => updates == 1); Equal(0, shown);
                service.ShowCurrentState(); PumpUntil(() => shown == 1);
                service.TogglePaused(); PumpUntil(() => updates == 3);
                state = state with { CapsMode = CapsMode.Upper }; service.Request(); PumpUntil(() => shown == 2);
            }),
            ("Late results after disposal are ignored", () => {
                using var gate = new ManualResetEventSlim(); var entered = false; var exited = false;
                var reader = new FakeReader(() => { entered = true; gate.Wait(); exited = true; return InputState.Unknown; });
                var service = NewService(reader); var notifications = 0;
                service.StateChanged += (_, _) => notifications++;
                service.StateUpdated += (_, _) => notifications++;
                service.ShowCurrentState(); PumpUntil(() => entered); service.Dispose(); gate.Set();
                PumpUntil(() => exited); PumpFor(100); Equal(0, notifications);
                service.ShowCurrentState(); PumpFor(30); Equal(0, notifications);
            }),
            ("Repeated pending refreshes never overlap readers", () => {
                int active = 0, max = 0, calls = 0, updates = 0;
                using var gate = new ManualResetEventSlim();
                using var service = NewService(new FakeReader(() => {
                    var n = Interlocked.Increment(ref active); max = Math.Max(max, n);
                    Interlocked.Increment(ref calls); gate.Wait(); Interlocked.Decrement(ref active); return InputState.Unknown;
                }));
                service.StateUpdated += (_, _) => updates++;
                service.Request(); PumpUntil(() => calls == 1);
                for (var i = 0; i < 100; i++) service.Request();
                gate.Set(); PumpUntil(() => updates == 2); Equal(1, max); Equal(2, calls);
            })
        };
        var failures = 0;
        foreach (var test in tests)
        {
            try { test.Body(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception ex) { failures++; Console.Error.WriteLine("FAIL " + test.Name + ": " + ex); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
        return failures == 0 ? 0 : 1;
    }
    private static InputStateService NewService(IInputStateReader reader) =>
        new(reader, new AppSettings(), () => new ForegroundContext(1, null, false, false));
    private static void PumpUntil(Func<bool> condition)
    {
        var deadline = Environment.TickCount64 + 5000;
        while (!condition())
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("Dispatcher condition timed out");
            PumpFor(5);
        }
    }
    private static void PumpFor(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static void InTemporaryDirectory(Action<string> test)
    {
        var dir = Path.Combine(Path.GetTempPath(), "SureType-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { test(dir); } finally { Directory.Delete(dir, true); }
    }
    private static void True(bool value) { if (!value) throw new InvalidOperationException("Assertion failed"); }
    private static void Equal<T>(T expected, T actual) {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}");
    }
    private sealed class FakeReader(Func<InputState> read) : IInputStateReader { public InputState ReadCurrentState() => read(); }
}
