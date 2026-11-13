using System.Diagnostics;
using Daily_Goals_Editor.Core;
using Microsoft.Maui.Controls.Shapes;

namespace Daily_Goals_Editor.UI;

public static class Ui
{
    public static Color ColorResource(string key) => (Color)Resource(key);

    public static Style StyleResource(string key) => (Style)Resource(key);

    public static Color CategoryColor(Category category) => ColorResource(category switch
    {
        Category.Product => "CategoryProduct",
        Category.Engineering => "CategoryEngineering",
        Category.Design => "CategoryDesign",
        Category.Operations => "CategoryOperations",
        _ => throw new UnreachableException($"Unhandled {nameof(Category)}: {category}")
    });

    public static Ellipse CategoryDot(Category category, double size) => new()
    {
        WidthRequest = size,
        HeightRequest = size,
        VerticalOptions = LayoutOptions.Center,
        Fill = new SolidColorBrush(CategoryColor(category))
    };

    /// <summary>Gives a borderless Entry (or any view) the design's input-field chrome.</summary>
    public static Border Input(View content) => new() { Style = StyleResource("Input"), Content = content };

    public static Label WithTextSize(this Label label, TextSize size)
    {
        label.SetDynamicResource(Label.FontSizeProperty, TextScale.ResourceKey(size));
        return label;
    }

    public static T OnTap<T>(this T view, Action action) where T : View
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(tap);
        return view;
    }

    public static Border Hover(this Border view, Color background, Color hoverBackground, Color? stroke = null, Color? hoverStroke = null)
    {
        view.BackgroundColor = background;
        if (stroke is not null)
        {
            view.Stroke = stroke;
        }

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) =>
        {
            view.BackgroundColor = hoverBackground;
            if (hoverStroke is not null)
            {
                view.Stroke = hoverStroke;
            }
        };
        pointer.PointerExited += (_, _) =>
        {
            view.BackgroundColor = background;
            if (stroke is not null)
            {
                view.Stroke = stroke;
            }
        };
        view.GestureRecognizers.Add(pointer);
        return view;
    }

    private static object Resource(string key) =>
        Application.Current is not null && Application.Current.Resources.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"No resource named '{key}' in Resources/Styles.");
}
