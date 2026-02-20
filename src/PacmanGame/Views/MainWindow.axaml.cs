using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PacmanGame.ViewModels;

namespace PacmanGame.Views;

public partial class MainWindow : Window
{
    private bool _pausedForConsole;

    public MainWindow()
    {
        InitializeComponent();
        this.ContentHost.PropertyChanged += OnContentChanged;
        this.KeyDown += OnKeyDown;
    }

    private void OnContentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentControl.ContentProperty && e.NewValue is GameView gameView)
        {
            // Use a delayed action to ensure the view is fully loaded and visible
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                gameView.Focus();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is MainWindowViewModel viewModel && viewModel.ConsoleViewModel != null && IsConsoleAvailable(viewModel))
            {
                ToggleConsole();
            }
            e.Handled = true;
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
