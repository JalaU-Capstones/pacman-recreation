using Microsoft.Extensions.Logging;
using PacmanGame.Models.Game;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.KeyBindings;
using System.Windows.Input;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Input;
using System.Threading.Tasks;
using System.Reactive;

namespace PacmanGame.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IProfileManager _profileManager;
    private readonly IAudioManager _audioManager;
    private readonly IKeyBindingService _keyBindings;
    private readonly ILogger<SettingsViewModel> _logger;

    private Profile? _activeProfile;
    public Profile? ActiveProfile
    {
        get => _activeProfile;
        set => this.RaiseAndSetIfChanged(ref _activeProfile, value);
    }

    private bool _isDeleteConfirmationVisible;
    public bool IsDeleteConfirmationVisible
    {
        get => _isDeleteConfirmationVisible;
        set => this.RaiseAndSetIfChanged(ref _isDeleteConfirmationVisible, value);
    }

    private bool _isMusicEnabled;
    public bool IsMusicEnabled
    {
        get => _isMusicEnabled;
        set => this.RaiseAndSetIfChanged(ref _isMusicEnabled, value);
    }

    private int _menuMusicVolume;
    public int MenuMusicVolume
    {
        get => _menuMusicVolume;
        set => this.RaiseAndSetIfChanged(ref _menuMusicVolume, value);
    }

    private int _gameMusicVolume;
    public int GameMusicVolume
    {
        get => _gameMusicVolume;
        set => this.RaiseAndSetIfChanged(ref _gameMusicVolume, value);
    }

    private int _sfxVolume;
    public int SfxVolume
    {
        get => _sfxVolume;
        set => this.RaiseAndSetIfChanged(ref _sfxVolume, value);
    }

    public ObservableCollection<KeyBindingCategoryViewModel> KeyBindingCategories { get; } = new();

    private bool _isKeyCaptureVisible;
    public bool IsKeyCaptureVisible
    {
        get => _isKeyCaptureVisible;
        set => this.RaiseAndSetIfChanged(ref _isKeyCaptureVisible, value);
    }

    private string _keyCaptureTitle = string.Empty;
    public string KeyCaptureTitle
    {
        get => _keyCaptureTitle;
        set => this.RaiseAndSetIfChanged(ref _keyCaptureTitle, value);
    }

    private string _keyCaptureErrorMessage = string.Empty;
    public string KeyCaptureErrorMessage
    {
        get => _keyCaptureErrorMessage;
        set => this.RaiseAndSetIfChanged(ref _keyCaptureErrorMessage, value);
    }

    private string _pendingAction = string.Empty;
    private Key _pendingKey;
    private KeyModifiers _pendingModifiers;

    private bool _isConflictDialogVisible;
    public bool IsConflictDialogVisible
    {
        get => _isConflictDialogVisible;
        set => this.RaiseAndSetIfChanged(ref _isConflictDialogVisible, value);
    }

    private string _conflictMessage = string.Empty;
    public string ConflictMessage
    {
        get => _conflictMessage;
        set => this.RaiseAndSetIfChanged(ref _conflictMessage, value);
    }

    private string _conflictAction = string.Empty;
    private string _conflictNewAction = string.Empty;

    public ICommand SwitchProfileCommand { get; }
    public ICommand ShowDeleteConfirmationCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand ReturnToMenuCommand { get; }

    public ReactiveCommand<string, Unit> BeginKeyCaptureCommand { get; }
    public ICommand CancelKeyCaptureCommand { get; }
    public ICommand ResetKeyBindingsCommand { get; }
    public ICommand CancelConflictCommand { get; }
    public ICommand ConfirmReassignCommand { get; }

    public SettingsViewModel(MainWindowViewModel mainWindowViewModel, IProfileManager profileManager, IAudioManager audioManager, IKeyBindingService keyBindings, ILogger<SettingsViewModel> logger)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _profileManager = profileManager;
        _audioManager = audioManager;
        _keyBindings = keyBindings;
        _logger = logger;

        _activeProfile = _profileManager.GetActiveProfile();
        _isMusicEnabled = !_audioManager.IsMuted;
        _mainWindowViewModel.IsMuted = _audioManager.IsMuted;
        _menuMusicVolume = (int)(_audioManager.MenuMusicVolume * 100);
        _gameMusicVolume = (int)(_audioManager.GameMusicVolume * 100);
        _sfxVolume = (int)(_audioManager.SfxVolume * 100);

        SwitchProfileCommand = ReactiveCommand.Create(SwitchProfile);
        ShowDeleteConfirmationCommand = ReactiveCommand.Create(() => IsDeleteConfirmationVisible = true);
        CancelDeleteCommand = ReactiveCommand.Create(() => IsDeleteConfirmationVisible = false);
        ConfirmDeleteCommand = ReactiveCommand.Create(ConfirmDelete);
        ReturnToMenuCommand = ReactiveCommand.Create(ReturnToMenu);

        BeginKeyCaptureCommand = ReactiveCommand.Create<string>(BeginKeyCapture);
        CancelKeyCaptureCommand = ReactiveCommand.Create(() =>
        {
            IsKeyCaptureVisible = false;
            KeyCaptureErrorMessage = string.Empty;
            _pendingAction = string.Empty;
        });
        ResetKeyBindingsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await _keyBindings.ResetToDefaultsAsync();
            RefreshKeyBindingTexts();
        });
        CancelConflictCommand = ReactiveCommand.Create(() =>
        {
            IsConflictDialogVisible = false;
            ConflictMessage = string.Empty;
            _conflictAction = string.Empty;
            _conflictNewAction = string.Empty;
        });
        ConfirmReassignCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (string.IsNullOrWhiteSpace(_conflictNewAction)) return;
            var result = await _keyBindings.TrySetBindingAsync(_conflictNewAction, _pendingKey, _pendingModifiers, reassignOnConflict: true);
            if (result.Success)
            {
                IsConflictDialogVisible = false;
                IsKeyCaptureVisible = false;
                RefreshKeyBindingTexts();
            }
        });

        this.WhenAnyValue(x => x.IsMusicEnabled).Subscribe(value =>
        {
            _audioManager.SetMuted(!value);
            _mainWindowViewModel.IsMuted = !value;
            SaveSettings();
        });

        this.WhenAnyValue(x => x.MenuMusicVolume).Subscribe(value =>
        {
            _audioManager.SetMenuMusicVolume(value / 100f);
            SaveSettings();
        });

        this.WhenAnyValue(x => x.GameMusicVolume).Subscribe(value =>
        {
            _audioManager.SetGameMusicVolume(value / 100f);
            SaveSettings();
        });

        this.WhenAnyValue(x => x.SfxVolume).Subscribe(value =>
        {
            _audioManager.SetSfxVolume(value / 100f);
            SaveSettings();
        });

        BuildKeyBindingCategories();
        _keyBindings.BindingsChanged += (_, _) => RefreshKeyBindingTexts();
    }

    public async Task CaptureKeyAsync(Key key, KeyModifiers modifiers)
    {
        if (!IsKeyCaptureVisible || string.IsNullOrWhiteSpace(_pendingAction))
        {
            return;
        }

        _pendingKey = key;
        _pendingModifiers = modifiers;

        var result = await _keyBindings.TrySetBindingAsync(_pendingAction, key, modifiers, reassignOnConflict: false);
        if (result.Success)
        {
            IsKeyCaptureVisible = false;
            KeyCaptureErrorMessage = string.Empty;
            RefreshKeyBindingTexts();
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.ConflictAction))
        {
            IsConflictDialogVisible = true;
            _conflictAction = result.ConflictAction;
            _conflictNewAction = _pendingAction;
            ConflictMessage = $"The key is already assigned to: {GetDisplayNameForAction(_conflictAction)}";
            return;
        }

        KeyCaptureErrorMessage = result.IsReserved
            ? "That key is reserved by the system."
            : result.IsModifierOnly
                ? "Modifier keys cannot be bound alone."
                : (result.Message ?? "Unable to bind that key.");
    }

    private void SaveSettings()
    {
        if (ActiveProfile == null) return;

        var settings = new Settings
        {
            ProfileId = ActiveProfile.Id,
            MenuMusicVolume = MenuMusicVolume / 100.0,
            GameMusicVolume = GameMusicVolume / 100.0,
            SfxVolume = SfxVolume / 100.0,
            IsMuted = !IsMusicEnabled
        };
        _profileManager.SaveSettings(ActiveProfile.Id, settings);
    }

    private void SwitchProfile()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<ProfileSelectionViewModel>();
    }

    private void ConfirmDelete()
    {
        if (ActiveProfile != null)
        {
            _audioManager.PlaySoundEffect("menu-select");
            _profileManager.DeleteProfile(ActiveProfile.Id);

            var profiles = _profileManager.GetAllProfiles();
            if (profiles.Count > 0)
            {
                _mainWindowViewModel.NavigateTo<ProfileSelectionViewModel>();
            }
            else
            {
                _mainWindowViewModel.NavigateTo<ProfileCreationViewModel>();
            }
        }
    }

    private void ReturnToMenu()
    {
        _audioManager.PlaySoundEffect("menu-select");
        _mainWindowViewModel.NavigateTo<MainMenuViewModel>();
    }

    private void BuildKeyBindingCategories()
    {
        KeyBindingCategories.Clear();

        AddCategory("Gameplay", new[]
        {
            (KeyBindingActions.MoveUp, "Move Up"),
            (KeyBindingActions.MoveDown, "Move Down"),
            (KeyBindingActions.MoveLeft, "Move Left"),
            (KeyBindingActions.MoveRight, "Move Right"),
            (KeyBindingActions.PauseGame, "Pause Game")
        });

        AddCategory("System", new[]
        {
            (KeyBindingActions.OpenConsole, "Open Console"),
            (KeyBindingActions.ShowFps, "Show FPS Counter"),
            (KeyBindingActions.MuteAudio, "Mute Audio"),
            (KeyBindingActions.Fullscreen, "Fullscreen Toggle")
        });

        AddCategory("Creative Mode", new[]
        {
            (KeyBindingActions.PlaceTile, "Place Tile"),
            (KeyBindingActions.DeleteTile, "Delete Tile"),
            (KeyBindingActions.RotateTile, "Rotate Tile"),
            (KeyBindingActions.CycleTools, "Cycle Tools"),
            (KeyBindingActions.PlayTest, "Play Test"),
            (KeyBindingActions.ExportProject, "Export Project"),
            (KeyBindingActions.ImportProject, "Import Project")
        });

        RefreshKeyBindingTexts();
    }

    private void AddCategory(string name, (string Action, string DisplayName)[] actions)
    {
        var category = new KeyBindingCategoryViewModel(name);
        foreach (var (action, displayName) in actions)
        {
            category.Rows.Add(new KeyBindingRowViewModel(action, displayName, () => _keyBindings.GetKeyText(action), BeginKeyCaptureCommand));
        }
        KeyBindingCategories.Add(category);
    }

    private void RefreshKeyBindingTexts()
    {
        foreach (var cat in KeyBindingCategories)
        {
            foreach (var row in cat.Rows)
            {
                row.Refresh();
            }
        }
    }

    private void BeginKeyCapture(string action)
    {
        _pendingAction = action;
        KeyCaptureTitle = $"Press a key for: {GetDisplayNameForAction(action)}";
        KeyCaptureErrorMessage = string.Empty;
        IsKeyCaptureVisible = true;
    }

    private string GetDisplayNameForAction(string action)
    {
        foreach (var cat in KeyBindingCategories)
        {
            var row = cat.Rows.FirstOrDefault(r => r.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
            if (row != null)
            {
                return row.DisplayName;
            }
        }
        return action;
    }
}

public sealed class KeyBindingCategoryViewModel
{
    public string Name { get; }
    public ObservableCollection<KeyBindingRowViewModel> Rows { get; } = new();

    public KeyBindingCategoryViewModel(string name)
    {
        Name = name;
    }
}

public sealed class KeyBindingRowViewModel : ViewModelBase
{
    private readonly Func<string> _getBindingText;
    public string Action { get; }
    public string DisplayName { get; }

    private string _bindingText = string.Empty;
    public string BindingText
    {
        get => _bindingText;
        set => this.RaiseAndSetIfChanged(ref _bindingText, value);
    }

    public ICommand ChangeCommand { get; }

    public KeyBindingRowViewModel(string action, string displayName, Func<string> getBindingText, ICommand changeCommand)
    {
        Action = action;
        DisplayName = displayName;
        _getBindingText = getBindingText;
        ChangeCommand = changeCommand;
        Refresh();
    }

    public void Refresh()
    {
        BindingText = _getBindingText();
    }
}
