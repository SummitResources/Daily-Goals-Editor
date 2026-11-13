using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Daily_Goals_Editor.Core;

namespace Daily_Goals_Editor.Web.Services;

/// <summary>
/// The browser counterpart of <see cref="DayPlanStore"/>: one plan per calendar day, kept in localStorage under a
/// key named for the date the app was opened, so each day starts with an empty list.
/// </summary>
public sealed class BrowserDayPlanStore(BrowserStorage storage)
{
    private const string KEY_PREFIX = "daily-goals:";

    private string _key = string.Empty;

    /// <summary>
    /// Returns a fresh plan on first run. A saved plan that can't be read throws, so a damaged list is never
    /// silently replaced by an empty one on the next save.
    /// </summary>
    public async Task<DayPlan> LoadAsync()
    {
        _key = KEY_PREFIX + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var json = await storage.GetAsync(_key);
        if (json is null)
        {
            return new DayPlan();
        }

        return JsonSerializer.Deserialize(json, DayPlanJsonContext.Default.DayPlan)
               ?? throw new InvalidDataException($"{_key} holds JSON null instead of a saved plan.");
    }

    public async Task SaveAsync(DayPlan plan) =>
        await storage.SetAsync(_key, JsonSerializer.Serialize(plan, DayPlanJsonContext.Default.DayPlan));
}

/// <summary>Source-generated so serialization survives the trimming a published WebAssembly app goes through.</summary>
[JsonSerializable(typeof(DayPlan))]
internal sealed partial class DayPlanJsonContext : JsonSerializerContext;
