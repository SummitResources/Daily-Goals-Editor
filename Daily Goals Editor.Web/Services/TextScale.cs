using System.Globalization;
using Microsoft.JSInterop;

namespace Daily_Goals_Editor.Web.Services;

/// <summary>
/// The user's text size, as a multiplier on the design's font sizes. It is published as the --text-scale CSS
/// variable that every font size in app.css multiplies by, so changing it resizes the whole page.
/// </summary>
public sealed class TextScale(BrowserStorage storage, IJSRuntime js)
{
    public const double MINIMUM = 0.8;
    public const double MAXIMUM = 1.5;
    public const double STEP = 0.1;

    private const string STORAGE_KEY = "TextScale";

    public double Current { get; private set; } = 1;

    public async Task LoadAsync()
    {
        var saved = await storage.GetAsync(STORAGE_KEY);
        await ApplyAsync(double.TryParse(saved, CultureInfo.InvariantCulture, out var scale) ? scale : 1);
    }

    public async Task SetAsync(double scale)
    {
        await ApplyAsync(scale);
        await storage.SetAsync(STORAGE_KEY, Current.ToString(CultureInfo.InvariantCulture));
    }

    private async Task ApplyAsync(double scale)
    {
        // Rounded so repeated steps land on exact tenths instead of drifting past the limits.
        Current = Math.Round(Math.Clamp(scale, MINIMUM, MAXIMUM), 1);
        await js.InvokeVoidAsync("dailyGoals.setTextScale", Current.ToString(CultureInfo.InvariantCulture));
    }
}
