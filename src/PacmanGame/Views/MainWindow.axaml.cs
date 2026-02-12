using Avalonia;
using Avalonia.Controls;
using PacmanGame.ViewModels;

namespace PacmanGame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.ContentHost.PropertyChanged += OnContentChanged;
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
}
