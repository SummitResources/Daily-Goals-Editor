using System.Diagnostics;

namespace Daily_Goals_Editor.UI;

public enum TextSize
{
    ExtraSmall,
    Small,
    Medium,
    Body,
    Large,
    Title,
    Heading
}

/// <summary>
/// The user's text size, as a multiplier on the design's font sizes. Each <see cref="TextSize"/> is published as an
/// app resource that fonts reference with DynamicResource, so changing the scale resizes every open view.
/// </summary>
public static class TextScale
{
    public const double MINIMUM = 0.8;
    public const double MAXIMUM = 1.5;
    public const double STEP = 0.1;

    private const string PREFERENCE_KEY = "TextScale";

    public static double Current { get; private set; } = 1;

    public static string ResourceKey(TextSize size) => $"TextSize{size}";

    public static void Load() => Apply(Preferences.Default.Get(PREFERENCE_KEY, 1d));

    public static void Set(double scale)
    {
        Apply(scale);
        Preferences.Default.Set(PREFERENCE_KEY, Current);
    }

    private static void Apply(double scale)
    {
        var resources = Application.Current?.Resources
                        ?? throw new InvalidOperationException("Text scale was applied before the application was created.");

        // Rounded so repeated steps land on exact tenths instead of drifting past the limits.
        Current = Math.Round(Math.Clamp(scale, MINIMUM, MAXIMUM), 1);
        foreach (var size in Enum.GetValues<TextSize>())
        {
            resources[ResourceKey(size)] = DesignPoints(size) * Current;
        }
    }

    private static double DesignPoints(TextSize size) => size switch
    {
        TextSize.ExtraSmall => 11,
        TextSize.Small => 12,
        TextSize.Medium => 13,
        TextSize.Body => 14,
        TextSize.Large => 16,
        TextSize.Title => 20,
        TextSize.Heading => 24,
        _ => throw new UnreachableException($"Unhandled {nameof(TextSize)}: {size}")
    };
}
