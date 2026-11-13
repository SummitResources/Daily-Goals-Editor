using System.Text.Json.Serialization;

namespace Daily_Goals_Editor.Core;

public sealed class DayTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Category Category { get; set; }
    public bool Complete { get; set; }
    public List<string> SubItems { get; init; } = [];
    public string? Title { get; set; }
    public string? Ticket { get; set; }

    [JsonIgnore]
    public string DisplayTitle => Title ?? Ticket ?? "Untitled task";

    /// <summary>How the prompt refers to the task: its ticket key, or its quoted title for a free-form task.</summary>
    [JsonIgnore]
    public string PromptName => Ticket ?? $"\"{DisplayTitle}\"";

    /// <summary>Blank sub-items are empty editor rows waiting to be filled in, so they don't count.</summary>
    [JsonIgnore]
    public int SubItemCount => SubItems.Count(subItem => !string.IsNullOrWhiteSpace(subItem));
}
