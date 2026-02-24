using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PacmanGame.ViewModels;
using PacmanGame.Services.Interfaces;
using PacmanGame.Services.KeyBindings;

namespace PacmanGame.Views;

public partial class MainWindow : Window
{
    private bool _pausedForConsole;
    private bool _didClampToWorkArea;

    public MainWindow()
    {
        InitializeComponent();
        this.ContentHost.PropertyChanged += OnContentChanged;
        this.KeyDown += OnKeyDown;
        this.Opened += (_, _) => ClampToWorkArea();
    }

    private void OnContentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentControl.ContentProperty && e.NewValue is GameView gameView)
        {
            // Use a delayed action to ensure the view is fully loaded and visible
            Dispatcher.UIThread.Post(() =>
            {
                gameView.Focus();
            }, DispatcherPriority.Loaded);
        }
    }

    private void ClampToWorkArea()
    {
        if (_didClampToWorkArea)
        {
            return;
        }

        _didClampToWorkArea = true;

        var screen = Screens?.ScreenFromWindow(this) ?? Screens?.Primary;
        if (screen == null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return;
        }

        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;

        // If SizeToContent made the window larger than the work area (common in VMs), clamp it.
        if (Width > MaxWidth)
        {
            Width = MaxWidth;
        }
        if (Height > MaxHeight)
        {
            Height = MaxHeight;
        }

        // After initial sizing, keep manual sizing so user resize behaves predictably.
        SizeToContent = SizeToContent.Manual;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var keyBindings = App.GetService<IKeyBindingService>();

        if ((keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.OpenConsole, e.Key, e.KeyModifiers))
            || (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            if (DataContext is MainWindowViewModel viewModel && viewModel.ConsoleViewModel != null && IsConsoleAvailable(viewModel))
            {
                ToggleConsole();
            }
            e.Handled = true;
            return;
        }

        if (keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.MuteAudio, e.Key, e.KeyModifiers))
        {
            ToggleMute();
            e.Handled = true;
            return;
        }

        if (keyBindings != null && keyBindings.IsActionTriggered(KeyBindingActions.Fullscreen, e.Key, e.KeyModifiers))
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
            return;
        }
    }

    private void ToggleMute()
    {
        var audio = App.GetService<IAudioManager>();
        var profiles = App.GetService<IProfileManager>();

        if (DataContext is not MainWindowViewModel viewModel || audio == null)
        {
            return;
        }

        var newMuted = !audio.IsMuted;
        audio.SetMuted(newMuted);
        viewModel.IsMuted = newMuted;

        var active = profiles?.GetActiveProfile();
        if (active != null && profiles != null)
        {
            var settings = profiles.LoadSettings(active.Id);
            settings.IsMuted = newMuted;
            profiles.SaveSettings(active.Id, settings);
        }
    }

    private void ToggleConsole()
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        if (viewModel.ConsoleViewModel == null) return;

        if (!IsConsoleAvailable(viewModel))
        {
            return;
        }

        var consoleOpening = !viewModel.ConsoleViewModel.IsVisible;

        if (consoleOpening && viewModel.CurrentViewModel is GameViewModel gameViewModel &&
            gameViewModel.IsGameRunning && !gameViewModel.IsPaused)
        {
            gameViewModel.PauseGameCommand.Execute(null);
            _pausedForConsole = true;
        }

        if (consoleOpening)
        {
            viewModel.ConsoleViewModel.Open();
        }
        else
        {
            viewModel.ConsoleViewModel.Close();
            if (_pausedForConsole && viewModel.CurrentViewModel is GameViewModel resumedGame &&
                resumedGame.IsPaused)
            {
                resumedGame.ResumeGameCommand.Execute(null);
            }
            _pausedForConsole = false;
        }
    }

    private static bool IsConsoleAvailable(MainWindowViewModel viewModel)
    {
        return viewModel.CurrentViewModel is not ProfileSelectionViewModel
            && viewModel.CurrentViewModel is not ProfileCreationViewModel;
    }
}
