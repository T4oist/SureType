using SureType.Models;

var tests = new List<(string Name, Action Body)>
{
    ("Unknown input state keeps fallback values", () =>
    {
        AssertEqual(InputSource.Unknown, InputState.Unknown.InputSource);
        AssertEqual(ImeMode.Unknown, InputState.Unknown.ImeMode);
        AssertEqual(CapsMode.Lower, InputState.Unknown.CapsMode);
    }),

    ("App settings clamp timing values", () =>
    {
        var settings = new AppSettings
        {
            OverlayDurationSeconds = 10,
            InputFocusCooldownSeconds = -5
        };

        AssertEqual(5.0, settings.OverlayDurationSeconds);
        AssertEqual(1.0, settings.InputFocusCooldownSeconds);
    })
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures > 0)
{
    Environment.Exit(1);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}");
    }
}
