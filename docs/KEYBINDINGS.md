# Custom Keybindings

Starting with `v1.0.2`, all keyboard controls can be customized per profile.

## Accessing Keybindings

1. Main Menu -> Settings
2. Open the `Keybindings` tab
3. Click `CHANGE` next to an action
4. Press the desired key combination
5. If a conflict is detected, choose `REASSIGN` or `CANCEL`

Changes apply immediately (no restart required).

## Default Keybindings

### Gameplay
- Move Up: Up
- Move Down: Down
- Move Left: Left
- Move Right: Right
- Pause Game: Escape

### System
- Open Console: Ctrl+C
- Show FPS Counter: F3
- Mute Audio: M
- Fullscreen Toggle: F11

### Creative Mode
- Place Tile: Enter
- Delete Tile: Delete
- Rotate Tile: R
- Cycle Tools: Tab
- Play Test: Ctrl+P
- Export Project: Ctrl+E
- Import Project: Ctrl+O

## Conflict Resolution

Bindings must be unique per profile. If you assign a key combination that is already used:
- A conflict dialog is shown.
- Choosing `REASSIGN` unbinds the previous action and assigns the key to the new action.

The previous action will display as `Unbound` until you assign a new key.

## Reserved Keys

Some keys are blocked because they are system-level shortcuts:
- Alt+F4
- Print Screen
- Windows key

Modifier keys alone (only Ctrl/Shift/Alt) cannot be bound.

## Reset To Defaults

Settings -> Keybindings -> `RESET TO DEFAULTS`

This restores all default keybindings for the active profile.

## Per-Profile

Each profile has its own keybinding set.
Switching profiles switches keybindings automatically.

## Storage

Keybindings are stored in the local encrypted profile database (SQLCipher) starting in `v1.0.2`.

