using Microsoft.JSInterop;

namespace Daily_Goals_Editor.Web.Services;

/// <summary>The browser's localStorage, which keeps an installed copy of the app working offline.</summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("dailyGoals.getItem", key);

    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("dailyGoals.setItem", key, value);
}
