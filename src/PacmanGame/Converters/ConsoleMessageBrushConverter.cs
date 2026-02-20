using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PacmanGame.Models.Console;

namespace PacmanGame.Converters;

public class ConsoleMessageBrushConverter : IValueConverter
{
    public static readonly IBrush DefaultBrush = Brushes.LightGray;
    public static readonly IBrush InfoBrush = Brushes.LightGray;
    public static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#55FF55"));
    public static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF5555"));
    public static readonly IBrush InputBrush = Brushes.White;
    public static readonly IBrush SystemBrush = new SolidColorBrush(Color.Parse("#FFD700"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ConsoleMessageType messageType) return DefaultBrush;

        return messageType switch
        {
            ConsoleMessageType.Info => InfoBrush,
            ConsoleMessageType.Success => SuccessBrush,
            ConsoleMessageType.Error => ErrorBrush,
            ConsoleMessageType.Input => InputBrush,
            ConsoleMessageType.System => SystemBrush,
            _ => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
