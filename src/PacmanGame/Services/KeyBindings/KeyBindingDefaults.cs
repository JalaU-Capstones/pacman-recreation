using Avalonia.Input;
using System.Collections.Generic;

namespace PacmanGame.Services.KeyBindings;

public static class KeyBindingDefaults
{
    public static readonly IReadOnlyDictionary<string, (Key Key, KeyModifiers Modifiers)> Defaults =
        new Dictionary<string, (Key, KeyModifiers)>
        {
            { KeyBindingActions.MoveUp, (Key.Up, KeyModifiers.None) },
            { KeyBindingActions.MoveDown, (Key.Down, KeyModifiers.None) },
            { KeyBindingActions.MoveLeft, (Key.Left, KeyModifiers.None) },
            { KeyBindingActions.MoveRight, (Key.Right, KeyModifiers.None) },
            { KeyBindingActions.PauseGame, (Key.Escape, KeyModifiers.None) },

            { KeyBindingActions.OpenConsole, (Key.C, KeyModifiers.Control) },
            { KeyBindingActions.ShowFps, (Key.F3, KeyModifiers.None) },
            { KeyBindingActions.MuteAudio, (Key.M, KeyModifiers.None) },
            { KeyBindingActions.Fullscreen, (Key.F11, KeyModifiers.None) },

            { KeyBindingActions.PlaceTile, (Key.Enter, KeyModifiers.None) },
            { KeyBindingActions.DeleteTile, (Key.Delete, KeyModifiers.None) },
            { KeyBindingActions.RotateTile, (Key.R, KeyModifiers.None) },
            { KeyBindingActions.CycleTools, (Key.Tab, KeyModifiers.None) },
            { KeyBindingActions.PlayTest, (Key.P, KeyModifiers.Control) },
            { KeyBindingActions.ExportProject, (Key.E, KeyModifiers.Control) },
            { KeyBindingActions.ImportProject, (Key.O, KeyModifiers.Control) }
        };
}

