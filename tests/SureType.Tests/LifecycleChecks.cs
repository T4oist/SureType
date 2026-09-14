using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using SureType.Models;
using SureType.Services;
using SureType.Windows;

internal static class LifecycleChecks
{
    public static int Run(string directory, string productPath, double minutes)
    {
        Directory.CreateDirectory(directory);
        var store = new SettingsStore(Path.Combine(directory, "isolated-settings.json"));
        // Background validation must not show cues over the user's active work.
        if (minutes > 0) store.Save(new AppSettings { HasCompletedOnboarding = true, ShowOnInputFocus = false, ShowOnStateChange = false });
        var app = new SureType.App(store, minutes > 0 ? "SureType.Validation." + Environment.ProcessId : "SureType");
        app.InitializeComponent();
        var failed = false;
        var completed = false;
        var started = Stopwatch.StartNew();
        using var process = Process.GetCurrentProcess();
        var initialCpu = process.TotalProcessorTime;
        var heartbeat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(minutes > 0 ? 60 : 1) };
        var logPath = Path.Combine(directory, "samples.jsonl");
        void WriteSample(string status)
        {
            process.Refresh();
            File.AppendAllText(logPath, JsonSerializer.Serialize(new {
                timestamp = DateTimeOffset.Now, status, elapsedSeconds = started.Elapsed.TotalSeconds,
                cpuSeconds = (process.TotalProcessorTime - initialCpu).TotalSeconds,
                workingSetBytes = process.WorkingSet64, privateBytes = process.PrivateMemorySize64,
                handles = process.HandleCount, threads = process.Threads.Count
            }) + Environment.NewLine);
        }
        app.DispatcherUnhandledException += (_, e) => {
            failed = true; File.WriteAllText(Path.Combine(directory, "failure.txt"), e.Exception.ToString());
            // The test must fail and exit, rather than keep a broken application running.
            e.Handled = true; app.Shutdown(1);
        };
        app.Startup += (_, _) => app.Dispatcher.BeginInvoke(async () =>
        {
            try
            {
                await Task.Delay(500);
                var windows = app.Windows.OfType<MainWindow>().ToArray();
                if (minutes == 0 && windows.Length != 1) throw new Exception("Expected a first-run settings window");
                foreach (var window in windows) window.Close();
                if (minutes == 0)
                {
                    using (var silent = Process.Start(new ProcessStartInfo(Path.GetFullPath(productPath), "--startup") { UseShellExecute = false, CreateNoWindow = true })!)
                    {
                        if (!await ExitWithin(silent)) throw new Exception("Duplicate startup did not exit");
                    }
                    await Task.Delay(250);
                    if (app.Windows.OfType<MainWindow>().Any()) throw new Exception("Duplicate autostart opened settings");
                    using (var manual = Process.Start(new ProcessStartInfo(Path.GetFullPath(productPath)) { UseShellExecute = false, CreateNoWindow = true })!)
                    {
                        if (!await ExitWithin(manual)) throw new Exception("Duplicate manual launch did not exit");
                    }
                    await Task.Delay(500);
                    if (app.Windows.OfType<MainWindow>().Count() != 1) throw new Exception("Duplicate manual launch did not reopen settings");
                    foreach (var window in app.Windows.OfType<MainWindow>().ToArray()) window.Close();
                    Console.WriteLine("PASS first-run window, close-to-tray, silent duplicate startup, manual duplicate activation");
                    WriteSample("smoke-passed");
                    completed = true;
                    app.Shutdown();
                    return;
                }
                WriteSample("running");
                heartbeat.Tick += (_, _) =>
                {
                    WriteSample("running");
                    if (started.Elapsed.TotalMinutes >= minutes)
                    {
                        WriteSample("completed");
                        completed = true;
                        File.WriteAllText(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(new {
                            completed = true, durationSeconds = started.Elapsed.TotalSeconds,
                            averageCpuPercent = (process.TotalProcessorTime - initialCpu).TotalSeconds / started.Elapsed.TotalSeconds / Environment.ProcessorCount * 100,
                            runtime = Environment.Version.ToString(), os = Environment.OSVersion.VersionString,
                            logicalProcessors = Environment.ProcessorCount,
                            limitation = "Isolated settings; automatic overlays disabled. Does not validate IME correctness, idle-only CPU, or full desktop compatibility."
                        }, new JsonSerializerOptions { WriteIndented = true }));
                        app.Shutdown();
                    }
                };
                heartbeat.Start();
            }
            catch (Exception ex)
            {
                failed = true; File.WriteAllText(Path.Combine(directory, "failure.txt"), ex.ToString());
                Console.Error.WriteLine(ex); app.Shutdown(1);
            }
        });
        app.Run();
        heartbeat.Stop();
        if (!completed && !failed) Console.Error.WriteLine("NOT RUN: another instance prevented this validation from starting.");
        return failed || !completed ? 1 : 0;
    }
    private static async Task<bool> ExitWithin(Process process)
    {
        var exited = process.WaitForExitAsync();
        if (await Task.WhenAny(exited, Task.Delay(5000)) == exited) return process.ExitCode == 0;
        process.Kill(); return false;
    }
}
