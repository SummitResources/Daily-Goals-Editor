using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace Daily_Goals_Editor.UI;

public enum ButtonVariant
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    Success
}

public sealed class UiButton : Border
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(UiButton), "", propertyChanged: (button, _, _) => ((UiButton)button).Apply());

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(IconKind), typeof(UiButton), IconKind.None, propertyChanged: (button, _, _) => ((UiButton)button).Apply());

    public static readonly BindableProperty VariantProperty = BindableProperty.Create(
        nameof(Variant), typeof(ButtonVariant), typeof(UiButton), ButtonVariant.Secondary, propertyChanged: (button, _, _) => ((UiButton)button).Apply());

    public static readonly BindableProperty CompactProperty = BindableProperty.Create(
        nameof(Compact), typeof(bool), typeof(UiButton), false, propertyChanged: (button, _, _) => ((UiButton)button).Apply());

    private readonly Icon _icon = new() { VerticalOptions = LayoutOptions.Center };
    private readonly Label _label = new() { FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center };
    private bool _isHovered;

    public event EventHandler? Clicked;

    public UiButton()
    {
        StrokeShape = new RoundRectangle { CornerRadius = 8 };
        StrokeThickness = 1;
        Content = new HorizontalStackLayout { Spacing = 6, Children = { _icon, _label } };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (IsEnabled)
            {
                Clicked?.Invoke(this, EventArgs.Empty);
            }
        };
        GestureRecognizers.Add(tap);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) =>
        {
            _isHovered = true;
            Apply();
        };
        pointer.PointerExited += (_, _) =>
        {
            _isHovered = false;
            Apply();
        };
        GestureRecognizers.Add(pointer);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsEnabled))
            {
                Apply();
            }
        };
        Apply();
    }

    public UiButton(string text, IconKind icon, ButtonVariant variant, Action onClick, bool compact = false) : this()
    {
        Text = text;
        Icon = icon;
        Variant = variant;
        Compact = compact;
        Clicked += (_, _) => onClick();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IconKind Icon
    {
        get => (IconKind)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool Compact
    {
        get => (bool)GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    private void Apply()
    {
        var showHover = _isHovered && IsEnabled;
        var (background, foreground, stroke) = Variant switch
        {
            ButtonVariant.Primary => (Ui.ColorResource(showHover ? "AccentHover" : "Accent"), Colors.White, Colors.Transparent),
            ButtonVariant.Secondary => (Ui.ColorResource(showHover ? "Hover" : "Surface"), Ui.ColorResource("InkSoft"), Ui.ColorResource("LineStrong")),
            ButtonVariant.Ghost => (showHover ? Ui.ColorResource("Hover") : Colors.Transparent, Ui.ColorResource(showHover ? "Ink" : "Muted"), Colors.Transparent),
            ButtonVariant.Danger => (showHover ? Ui.ColorResource("DangerTint") : Colors.Transparent, Ui.ColorResource(showHover ? "Danger" : "Muted"), Colors.Transparent),
            ButtonVariant.Success => (Ui.ColorResource("SuccessTint"), Ui.ColorResource("Success"), Colors.Transparent),
            _ => throw new UnreachableException($"Unhandled {nameof(ButtonVariant)}: {Variant}")
        };

        BackgroundColor = background;
        Stroke = stroke;
        Opacity = IsEnabled ? 1 : 0.35;

        var hasText = !string.IsNullOrEmpty(Text);
        _label.Text = Text;
        _label.TextColor = foreground;
        _label.IsVisible = hasText;
        _label.WithTextSize(Compact ? TextSize.Small : TextSize.Medium);

        _icon.Kind = Icon;
        _icon.Color = foreground;
        _icon.Size = Compact || !hasText ? 15 : 17;

        if (!hasText)
        {
            Padding = new Thickness(6);
        }
        else if (Compact)
        {
            Padding = new Thickness(10, 6);
        }
        else
        {
            Padding = new Thickness(14, 9);
        }
    }
}
