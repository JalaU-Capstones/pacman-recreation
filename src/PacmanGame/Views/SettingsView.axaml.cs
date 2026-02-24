using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PacmanGame.ViewModels;

namespace PacmanGame.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Focusable = true;
        this.AttachedToVisualTree += (_, _) => this.Focus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        if (!vm.IsKeyCaptureVisible) return;
        if (vm.IsConflictDialogVisible) return;

        // Esc cancels capture.
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            vm.CancelKeyCaptureCommand.Execute(null);
            e.Handled = true;
            return;
        }

        await vm.CaptureKeyAsync(e.Key, e.KeyModifiers);
        e.Handled = true;
    }
}
