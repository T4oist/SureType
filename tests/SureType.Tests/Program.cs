using SureType.Models;
using SureType.Services;

var tests = new List<(string Name, Action Body)>
{
    ("Chinese IME Chinese mode maps to Chinese asset", () =>
        AssertEqual(InputStateAssets.ChineseInput, new InputState(InputSource.ChineseIme, ImeMode.Chinese, CapsMode.Lower).DisplayAsset)),

    ("Chinese IME English mode maps to dedicated asset", () =>
        AssertEqual(InputStateAssets.ChineseImeEnglish, new InputState(InputSource.ChineseIme, ImeMode.English, CapsMode.Upper).DisplayAsset)),

    ("English lower maps to lower asset", () =>
        AssertEqual(InputStateAssets.EnglishLower, new InputState(InputSource.EnglishKeyboard, ImeMode.NotApplicable, CapsMode.Lower).DisplayAsset)),

    ("English upper maps to upper asset", () =>
        AssertEqual(InputStateAssets.EnglishUpper, new InputState(InputSource.EnglishKeyboard, ImeMode.NotApplicable, CapsMode.Upper).DisplayAsset)),

    ("Unknown maps to fallback asset", () =>
        AssertEqual(InputStateAssets.Unknown, InputState.Unknown.DisplayAsset)),

    ("Typing cue policy cues first activity", () =>
    {
        var policy = new TypingCuePolicy(TimeSpan.FromSeconds(2));
        AssertTrue(policy.ShouldCue(DateTimeOffset.UnixEpoch));
    }),

    ("Typing cue policy suppresses repeated activity within cooldown", () =>
    {
        var policy = new TypingCuePolicy(TimeSpan.FromSeconds(2));
        AssertTrue(policy.ShouldCue(DateTimeOffset.UnixEpoch));
        AssertFalse(policy.ShouldCue(DateTimeOffset.UnixEpoch.AddMilliseconds(500)));
    }),

    ("Typing cue policy cues again after cooldown", () =>
    {
        var policy = new TypingCuePolicy(TimeSpan.FromSeconds(2));
        AssertTrue(policy.ShouldCue(DateTimeOffset.UnixEpoch));
        AssertTrue(policy.ShouldCue(DateTimeOffset.UnixEpoch.AddSeconds(3)));
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

static void AssertTrue(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true, got false");
    }
}

static void AssertFalse(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false, got true");
    }
}