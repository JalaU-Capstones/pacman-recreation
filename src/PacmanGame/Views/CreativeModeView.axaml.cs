using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PacmanGame.ViewModels.Creative;
using System.Reactive;
using System;
using PacmanGame.Services.KeyBindings;

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

        if (vm.IsActionTriggered(KeyBindingActions.MoveUp, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.MoveCursor(0, -1);
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.MoveDown, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.MoveCursor(0, 1);
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.MoveLeft, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.MoveCursor(-1, 0);
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.MoveRight, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.MoveCursor(1, 0);
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.PlaceTile, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.HandleCellActionAtCursor();
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.DeleteTile, e.Key, e.KeyModifiers) || e.Key == Key.Back)
        {
            vm.CanvasViewModel.ClearCell();
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.CycleTools, e.Key, e.KeyModifiers))
        {
            vm.ToolboxViewModel.CycleToNextTool();
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.RotateTile, e.Key, e.KeyModifiers))
        {
            vm.CanvasViewModel.RotateCurrentCell();
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.PlayTest, e.Key, e.KeyModifiers))
        {
            vm.PlayTestCommand.Execute().Subscribe(Observer.Create<System.Reactive.Unit>(_ => { }));
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.ExportProject, e.Key, e.KeyModifiers))
        {
            vm.ExportCommand.Execute().Subscribe(Observer.Create<System.Reactive.Unit>(_ => { }));
            e.Handled = true;
            return;
        }
        if (vm.IsActionTriggered(KeyBindingActions.ImportProject, e.Key, e.KeyModifiers))
        {
            vm.ImportCommand.Execute().Subscribe(Observer.Create<System.Reactive.Unit>(_ => { }));
            e.Handled = true;
            return;
        }

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
                vm.CanvasViewModel.HandleCellActionAtCursor();
                break;
            case Key.Delete:
                vm.CanvasViewModel.ClearCell();
                break;
            case Key.Back:
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

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not CreativeModeViewModel vm) return;
        if (sender is not Control canvasLayer) return;

        var position = e.GetPosition(canvasLayer);
        var zoom = Math.Max(0.01, vm.ZoomLevel);
        var unscaledX = position.X / zoom;
        var unscaledY = position.Y / zoom;

        var cellSize = LevelCanvasViewModel.TotalCellSize;
        var gridX = (int)Math.Floor(unscaledX / cellSize);
        var gridY = (int)Math.Floor(unscaledY / cellSize);

        vm.CanvasViewModel.HandleCellClick(gridX, gridY);
        e.Handled = true;
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
