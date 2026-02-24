using Avalonia.Input;
using Dapper;
using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.KeyBindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PacmanGame.Services;

public sealed class KeyBindingService : IKeyBindingService
{
    private readonly ProfileManager _profileManager;
    private readonly ILogger<KeyBindingService> _logger;

    private readonly Dictionary<int, Dictionary<string, KeyBindingEntry>> _cacheByProfileId = new();
    private bool _isInitialized;

    public event EventHandler? BindingsChanged;

    public KeyBindingService(ProfileManager profileManager, ILogger<KeyBindingService> logger)
    {
        _profileManager = profileManager;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        // ProfileManager is the schema owner; we only ensure reads/writes work.
        await Task.CompletedTask;
        _isInitialized = true;
    }

    public IReadOnlyDictionary<string, KeyBindingEntry> GetBindingsForActiveProfile()
    {
        var profileId = _profileManager.GetActiveProfile()?.Id;
        if (!profileId.HasValue)
        {
            return new Dictionary<string, KeyBindingEntry>();
        }

        EnsureLoaded(profileId.Value);
        return _cacheByProfileId[profileId.Value];
    }

    public string GetKeyText(string action)
    {
        var binding = GetBinding(action);
        if (binding == null)
        {
            return "Unbound";
        }

        if (!Enum.TryParse<Key>(binding.KeyCode, ignoreCase: true, out var key))
        {
            return "Unbound";
        }

        var mods = ParseModifiers(binding.ModifierKeys);
        return FormatKeyText(key, mods);
    }

    public bool IsActionTriggered(string action, Key key, KeyModifiers modifiers)
    {
        var binding = GetBinding(action);
        if (binding == null)
        {
            return false;
        }

        if (!Enum.TryParse<Key>(binding.KeyCode, ignoreCase: true, out var boundKey))
        {
            return false;
        }

        var boundMods = ParseModifiers(binding.ModifierKeys);
        return boundKey == key && NormalizeModifiers(boundMods) == NormalizeModifiers(modifiers);
    }

    public async Task<KeyBindingSetResult> TrySetBindingAsync(string action, Key key, KeyModifiers modifiers, bool reassignOnConflict)
    {
        var profile = _profileManager.GetActiveProfile();
        if (profile == null)
        {
            return new KeyBindingSetResult { Success = false, Message = "No active profile." };
        }

        if (IsReserved(key, modifiers))
        {
            return new KeyBindingSetResult { Success = false, IsReserved = true, Message = "That key is reserved by the system." };
        }

        if (IsModifierOnly(key))
        {
            return new KeyBindingSetResult { Success = false, IsModifierOnly = true, Message = "Modifier keys cannot be bound alone." };
        }

        EnsureLoaded(profile.Id);
        var normalizedMods = NormalizeModifiers(modifiers);

        var conflict = _cacheByProfileId[profile.Id]
            .FirstOrDefault(kvp =>
            {
                if (string.Equals(kvp.Key, action, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!Enum.TryParse<Key>(kvp.Value.KeyCode, ignoreCase: true, out var otherKey))
                {
                    return false;
                }

                var otherMods = NormalizeModifiers(ParseModifiers(kvp.Value.ModifierKeys));
                return otherKey == key && otherMods == normalizedMods;
            });

        if (!string.IsNullOrEmpty(conflict.Key) && !reassignOnConflict)
        {
            return new KeyBindingSetResult
            {
                Success = false,
                ConflictAction = conflict.Key,
                Message = $"Key is already assigned to {conflict.Key}."
            };
        }

        try
        {
            await using var connection = await _profileManager.OpenConnectionForServicesAsync();

            if (!string.IsNullOrEmpty(conflict.Key) && reassignOnConflict)
            {
                await connection.ExecuteAsync(
                    "DELETE FROM KeyBindings WHERE ProfileId = @ProfileId AND Action = @Action",
                    new { ProfileId = profile.Id, Action = conflict.Key });
                _cacheByProfileId[profile.Id].Remove(conflict.Key);
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var modText = SerializeModifiers(normalizedMods);

            // Upsert
            await connection.ExecuteAsync(@"
                INSERT INTO KeyBindings (ProfileId, Action, KeyCode, ModifierKeys, CreatedAt, UpdatedAt)
                VALUES (@ProfileId, @Action, @KeyCode, @ModifierKeys, @CreatedAt, @UpdatedAt)
                ON CONFLICT(ProfileId, Action) DO UPDATE SET
                    KeyCode = excluded.KeyCode,
                    ModifierKeys = excluded.ModifierKeys,
                    UpdatedAt = excluded.UpdatedAt;
            ", new
            {
                ProfileId = profile.Id,
                Action = action,
                KeyCode = key.ToString(),
                ModifierKeys = modText,
                CreatedAt = now,
                UpdatedAt = now
            });

            _cacheByProfileId[profile.Id][action] = new KeyBindingEntry
            {
                ProfileId = profile.Id,
                Action = action,
                KeyCode = key.ToString(),
                ModifierKeys = modText,
                CreatedAt = now,
                UpdatedAt = now
            };

            BindingsChanged?.Invoke(this, EventArgs.Empty);
            return new KeyBindingSetResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set keybinding {Action} for profile {ProfileId}", action, profile.Id);
            return new KeyBindingSetResult { Success = false, Message = "Failed to save keybinding." };
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        var profile = _profileManager.GetActiveProfile();
        if (profile == null) return;

        try
        {
            await using var connection = await _profileManager.OpenConnectionForServicesAsync();
            await connection.ExecuteAsync("DELETE FROM KeyBindings WHERE ProfileId = @ProfileId", new { ProfileId = profile.Id });

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var kvp in KeyBindingDefaults.Defaults)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO KeyBindings (ProfileId, Action, KeyCode, ModifierKeys, CreatedAt, UpdatedAt)
                    VALUES (@ProfileId, @Action, @KeyCode, @ModifierKeys, @CreatedAt, @UpdatedAt)
                    ON CONFLICT(ProfileId, Action) DO UPDATE SET
                        KeyCode = excluded.KeyCode,
                        ModifierKeys = excluded.ModifierKeys,
                        UpdatedAt = excluded.UpdatedAt;
                ", new
                {
                    ProfileId = profile.Id,
                    Action = kvp.Key,
                    KeyCode = kvp.Value.Key.ToString(),
                    ModifierKeys = SerializeModifiers(NormalizeModifiers(kvp.Value.Modifiers)),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            _cacheByProfileId.Remove(profile.Id);
            EnsureLoaded(profile.Id);
            BindingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset keybindings to defaults for profile {ProfileId}", profile.Id);
        }
    }

    private KeyBindingEntry? GetBinding(string action)
    {
        var profileId = _profileManager.GetActiveProfile()?.Id;
        if (!profileId.HasValue)
        {
            return null;
        }

        EnsureLoaded(profileId.Value);
        return _cacheByProfileId[profileId.Value].TryGetValue(action, out var binding) ? binding : null;
    }

    private void EnsureLoaded(int profileId)
    {
        if (_cacheByProfileId.ContainsKey(profileId))
        {
            return;
        }

        try
        {
            using var connection = _profileManager.OpenConnectionForServices();
            var bindings = connection.Query<KeyBindingEntry>(
                "SELECT Id, ProfileId, Action, KeyCode, ModifierKeys, CreatedAt, UpdatedAt FROM KeyBindings WHERE ProfileId = @ProfileId",
                new { ProfileId = profileId }).ToList();

            var dict = new Dictionary<string, KeyBindingEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in bindings)
            {
                if (!string.IsNullOrWhiteSpace(b.Action) && !string.IsNullOrWhiteSpace(b.KeyCode))
                {
                    dict[b.Action] = b;
                }
            }

            _cacheByProfileId[profileId] = dict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load keybindings for profile {ProfileId}", profileId);
            _cacheByProfileId[profileId] = new Dictionary<string, KeyBindingEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static KeyModifiers NormalizeModifiers(KeyModifiers mods)
    {
        return mods & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt);
    }

    private static bool IsModifierOnly(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }

    private static bool IsReserved(Key key, KeyModifiers modifiers)
    {
        var mods = NormalizeModifiers(modifiers);
        if (key is Key.LWin or Key.RWin)
        {
            return true;
        }

        if (key == Key.PrintScreen)
        {
            return true;
        }

        if (key == Key.F4 && mods.HasFlag(KeyModifiers.Alt))
        {
            return true;
        }

        return false;
    }

    private static string? SerializeModifiers(KeyModifiers modifiers)
    {
        modifiers = NormalizeModifiers(modifiers);
        if (modifiers == KeyModifiers.None)
        {
            return null;
        }

        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        return string.Join("+", parts);
    }

    private static KeyModifiers ParseModifiers(string? modifierKeys)
    {
        if (string.IsNullOrWhiteSpace(modifierKeys))
        {
            return KeyModifiers.None;
        }

        var mods = KeyModifiers.None;
        var parts = modifierKeys.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Control;
            }
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Shift;
            }
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                mods |= KeyModifiers.Alt;
            }
        }
        return mods;
    }

    private static string FormatKeyText(Key key, KeyModifiers modifiers)
    {
        var mods = SerializeModifiers(modifiers);
        var keyText = key switch
        {
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Enter => "Enter",
            _ => key.ToString()
        };

        return string.IsNullOrWhiteSpace(mods) ? keyText : $"{mods}+{keyText}";
    }
}
