# SureType

SureType is a lightweight Windows tray app that shows the current input state before you type. It helps you avoid typing into a chat box or editor with the wrong Chinese/English mode or CapsLock state.

## Features

- Detects the active keyboard layout or input source for the foreground window.
- Supports Chinese IME detection and tries to distinguish Chinese mode from English mode inside the Chinese IME.
- Detects English lowercase and CapsLock uppercase states.
- Shows a quiet top-right overlay when an input field is focused, with a cooldown so normal typing does not keep triggering popups.
- Can optionally show the overlay when the input state or CapsLock state changes.
- Runs from the system tray.
- Double-click the tray icon, or choose `Open SureType`, to open the icon guide and settings window.
- The settings window opens automatically when SureType starts.

## Icon Guide

SureType uses simple, low-noise icons:

- Green `Chinese` icon: Chinese IME is in Chinese input mode.
- Blue `EN` icon: a Chinese IME is active, but it is currently in English mode.
- Gray `en` icon: English input with CapsLock off.
- Dark `A` icon: English input with CapsLock on.
- Brown `?` icon: SureType cannot read the current input state.

The icon resources live in `src/SureType/Resources/StatusAssets.xaml`.

## Settings

Open the SureType window from the tray menu to adjust:

- Logo position: top right, top left, bottom right, or bottom left.
- Display time.
- Input-focus cooldown.
- Logo style: Filled, Soft, or Mono.
- Whether state changes should show the indicator.

Settings currently apply immediately for the running session. Persistent settings can be added later.

## Requirements

- Windows 10/11 x64.
- .NET 8 SDK for development.
- The self-contained release executable does not require .NET to be installed on the target machine.

## Build

```powershell
dotnet build
```

## Test

```powershell
dotnet run --project tests/SureType.Tests/SureType.Tests.csproj -c Release
```

The current tests cover input-state-to-icon mapping and the input-focus cooldown policy.

## Package as a Single EXE

```powershell
.\publish-self-contained.ps1
```

Output:

```text
artifacts\SureType-win-x64-single-exe\SureType.exe
```

The package is a compressed, self-contained, single-file Windows x64 executable. If SureType is already running, exit it from the tray before packaging again, otherwise the old EXE may be locked and cannot be overwritten.

## Current Limitations

- Chinese IME Chinese/English mode depends on the standard Windows IMM/IME conversion mode. Some third-party IMEs may not expose this state.
- Input-field focus detection uses Windows UI Automation. Most native and modern apps work, but some custom-drawn controls may not report focus correctly.
- Windows x64 only.

## Project Layout

```text
src/SureType/              WPF tray app
src/SureType/Models/       input state and app settings
src/SureType/Services/     input detection, focus hook, tray service
src/SureType/Windows/      overlay and settings/guide windows
src/SureType/Resources/    built-in icon resources
tests/SureType.Tests/      lightweight test runner
```

## License

No license has been selected yet.