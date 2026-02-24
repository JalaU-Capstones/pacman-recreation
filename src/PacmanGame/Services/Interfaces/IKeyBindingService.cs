using Avalonia.Input;
using PacmanGame.Models.Game;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PacmanGame.Services.Interfaces;

public interface IKeyBindingService
{
    event EventHandler? BindingsChanged;

    Task InitializeAsync();
    IReadOnlyDictionary<string, KeyBindingEntry> GetBindingsForActiveProfile();

    string GetKeyText(string action);
    bool IsActionTriggered(string action, Key key, KeyModifiers modifiers);

    Task<KeyBindingSetResult> TrySetBindingAsync(string action, Key key, KeyModifiers modifiers, bool reassignOnConflict);
    Task ResetToDefaultsAsync();
}

public sealed class KeyBindingSetResult
{
    public bool Success { get; init; }
    public bool IsReserved { get; init; }
    public bool IsModifierOnly { get; init; }
    public string? ConflictAction { get; init; }
    public string? Message { get; init; }
}
