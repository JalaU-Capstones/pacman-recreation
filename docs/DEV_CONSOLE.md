# DevConsole

**Release target:** v1.0.1

## Goal

Provide an in-game developer console overlay to speed up testing and debugging without attaching a debugger.

## Requirements (v1.0.1 scope)

- Toggle overlay with a single shortcut (default: `F12`).
- Command input with history (Up/Down), autocomplete optional.
- Non-destructive, game-safe commands only (no file writes outside app data).
- Logs displayed in the console (integrates with `Microsoft.Extensions.Logging`).

## Proposed Commands

- `help`: list commands.
- `active <view>`: navigate to a view (e.g. `active creative`, `active scoreboard`).
- `profile`: show active profile info (name, ids, progression flags).
- `score set <value>`: set score for rapid UI testing.
- `lives set <value>`: set lives for rapid UI testing.
- `level load <n>`: load built-in level `n`.
- `audio mute` / `audio unmute`: toggle audio output.

## Integration Notes

- Overlay should be a View bound to a ViewModel (MVVM) and opened from `MainWindowViewModel`.
- Commands should be implemented as services for testability (parsing + execution separated).
- All diagnostics output should go through `ILogger` (no `Console.WriteLine`).

