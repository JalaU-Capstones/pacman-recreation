using Avalonia.Data.Converters;
using Avalonia.Media;
using PacmanGame.Models.Creative;
using System;
using System.Globalization;

namespace PacmanGame.Converters;

public class CreativeCellBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CreativeTileType.Wall => new SolidColorBrush(Color.Parse("#4C4C4C")),
            CreativeTileType.Dot => new SolidColorBrush(Color.Parse("#CCCCCC")),
            CreativeTileType.PowerPellet => new SolidColorBrush(Color.Parse("#FFD700")),
            CreativeTileType.GhostSpawn => new SolidColorBrush(Color.Parse("#FF55FF")),
            CreativeTileType.Fruit => new SolidColorBrush(Color.Parse("#FF3333")),
            CreativeTileType.PacmanSpawn => new SolidColorBrush(Color.Parse("#00FF00")),
            _ => new SolidColorBrush(Color.Parse("#303030")),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
