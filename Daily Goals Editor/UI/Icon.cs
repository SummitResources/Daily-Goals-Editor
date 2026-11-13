using Microsoft.Maui.Controls.Shapes;

namespace Daily_Goals_Editor.UI;

public enum IconKind
{
    None,
    Check,
    Copy,
    Plus,
    Trash,
    ChevronUp,
    ChevronDown,
    List
}

/// <summary>The design's stroke icons: 24×24 viewBox paths scaled to <see cref="Size"/>.</summary>
public sealed class Icon : ContentView
{
    private static readonly Dictionary<IconKind, string> PATH_DATA = new()
    {
        [IconKind.Check] = "M5 12 L9 16 L19 6",
        [IconKind.Copy] =
            "M11 9 H20 A2 2 0 0 1 22 11 V20 A2 2 0 0 1 20 22 H11 A2 2 0 0 1 9 20 V11 A2 2 0 0 1 11 9 Z " +
            "M5 15 H4 A2 2 0 0 1 2 13 V4 A2 2 0 0 1 4 2 H13 A2 2 0 0 1 15 4 V5",
        [IconKind.Plus] = "M12 5 V19 M5 12 H19",
        [IconKind.Trash] = "M3 6 H21 M8 6 V4 H16 V6 M19 6 L18 21 H6 L5 6 M10 11 V17 M14 11 V17",
        [IconKind.ChevronUp] = "M18 15 L12 9 L6 15",
        [IconKind.ChevronDown] = "M6 9 L12 15 L18 9",
        [IconKind.List] = "M8 6 H21 M8 12 H21 M8 18 H21 M3 6 H3.01 M3 12 H3.01 M3 18 H3.01"
    };

    public static readonly BindableProperty KindProperty = BindableProperty.Create(
        nameof(Kind), typeof(IconKind), typeof(Icon), IconKind.None, propertyChanged: (icon, _, _) => ((Icon)icon).Apply());

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(Icon), 18d, propertyChanged: (icon, _, _) => ((Icon)icon).Apply());

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(Icon), Colors.Black, propertyChanged: (icon, _, _) => ((Icon)icon).Apply());

    private readonly Microsoft.Maui.Controls.Shapes.Path _path = new()
    {
        Aspect = Stretch.None,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeThickness = 1.8,
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Start
    };

    public Icon()
    {
        InputTransparent = true;
        Content = _path;
        Apply();
    }

    public Icon(IconKind kind, double size, Color color) : this()
    {
        Kind = kind;
        Size = size;
        Color = color;
    }

    public IconKind Kind
    {
        get => (IconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private void Apply()
    {
        WidthRequest = HeightRequest = Size;
        _path.WidthRequest = _path.HeightRequest = Size;
        _path.RenderTransform = new ScaleTransform(Size / 24, Size / 24);
        _path.Stroke = new SolidColorBrush(Color);
        IsVisible = Kind != IconKind.None;
        _path.Data = Kind == IconKind.None
            ? null
            : (Geometry?)new PathGeometryConverter().ConvertFromInvariantString(PATH_DATA[Kind]);
    }
}
