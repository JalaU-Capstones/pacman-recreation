using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PacmanGame.ViewModels.Creative;
using System.Reactive;

namespace PacmanGame.Views;

public partial class CreativeModeView : UserControl
{
    public CreativeModeView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, _) => this.Focus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CreativeModeViewModel vm) return;

        switch (e.Key)
        {
            case Key.Up:
                vm.CanvasViewModel.MoveCursor(0, -1);
                break;
            case Key.Down:
                vm.CanvasViewModel.MoveCursor(0, 1);
                break;
            case Key.Left:
                vm.CanvasViewModel.MoveCursor(-1, 0);
                break;
            case Key.Right:
                vm.CanvasViewModel.MoveCursor(1, 0);
                break;
            case Key.Enter:
                vm.CanvasViewModel.PlaceTool();
                break;
            case Key.Delete:
                vm.CanvasViewModel.ClearCell();
                break;
            case Key.Tab:
                vm.ToolboxViewModel.CycleToNextTool();
                e.Handled = true;
                break;
            case Key.R:
                vm.CanvasViewModel.RotateCurrentCell();
                break;
            default:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.P)
                {
                    var observer = Observer.Create<System.Reactive.Unit>(_ => { });
                    vm.PlayTestCommand.Execute().Subscribe(observer);
                }
                else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.E)
                {
                    var observer = Observer.Create<System.Reactive.Unit>(_ => { });
                    vm.ExportCommand.Execute().Subscribe(observer);
                }
                break;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not CreativeModeViewModel vm) return;

        // Use Ctrl + wheel for zoom; leave plain wheel for scrolling.
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (e.Delta.Y > 0)
        {
            vm.ZoomInCommand.Execute().Subscribe(Observer.Create<System.Reactive.Unit>(_ => { }));
        }
        else if (e.Delta.Y < 0)
        {
            vm.ZoomOutCommand.Execute().Subscribe(Observer.Create<System.Reactive.Unit>(_ => { }));
        }

        e.Handled = true;
    }
}
