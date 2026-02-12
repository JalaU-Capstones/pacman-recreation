using Avalonia.Controls;
using Avalonia.Input;
using PacmanGame.Models.Enums;
using PacmanGame.ViewModels;

namespace PacmanGame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainVm) return;

        var direction = e.Key switch
        {
            Key.Up => Direction.Up,
            Key.Down => Direction.Down,
            Key.Left => Direction.Left,
            Key.Right => Direction.Right,
            _ => Direction.None
        };

        if (direction != Direction.None)
        {
            if (mainVm.CurrentViewModel is GameViewModel gvm)
            {
                gvm.SetDirectionCommand.Execute(direction);
                e.Handled = true;
            }
            else if (mainVm.CurrentViewModel is MultiplayerGameViewModel mgvm)
            {
                mgvm.SetDirectionCommand.Execute(direction);
                e.Handled = true;
            }
        }
    }
}
