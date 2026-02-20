using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PacmanGame.Models.Creative;
using System;
using System.Globalization;

namespace PacmanGame.Converters;

public class ToolIconGeometryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ToolType tool)
        {
            return Geometry.Parse("M0,0");
        }

        return tool switch
        {
            ToolType.WallBlock => Geometry.Parse("M4,4 L32,4 L32,32 L4,32 Z"),
            ToolType.WallLine => Geometry.Parse("M5,19 H31"),
            ToolType.WallCorner => Geometry.Parse("M6,32 L6,6 L32,6"),
            ToolType.GhostHouse => Geometry.Parse("M6,28 L30,28 L30,14 C30,11 28,6 22,6 L14,6 C8,6 6,11 6,14 Z"),
            ToolType.PowerPellet => new EllipseGeometry(new Rect(5,5,26,26)),
            ToolType.Dot => new EllipseGeometry(new Rect(12,12,12,12)),
            _ => Geometry.Parse("M0,0")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
