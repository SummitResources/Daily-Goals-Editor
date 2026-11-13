using System.Globalization;
using System.Text.Json;

namespace Daily_Goals_Editor.Core;

public sealed class DayPlanStore(string directory)
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

    private const string FileNameSuffix = "daily-goals.json";

    private string _filePath = string.Empty;

    /// <summary>
    /// Returns a fresh plan on first run. A file that exists but can't be read throws, so a damaged list is
    /// never silently replaced by an empty one on the next save.
    /// </summary>
    public DayPlan Load()
    {
        _filePath = GenerateFileName();
        if (!File.Exists(_filePath))
        {
            return new DayPlan();
        }

        return JsonSerializer.Deserialize<DayPlan>(File.ReadAllText(_filePath), JSON_OPTIONS)
               ?? throw new InvalidDataException($"{_filePath} holds JSON null instead of a saved plan.");
    }

    /// <summary>Writes to a temp file and moves it into place, so a crash mid-write can't truncate the list.</summary>
    public void Save(DayPlan plan)
    {
        Directory.CreateDirectory(directory);
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(plan, JSON_OPTIONS));
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private string GenerateFileName()
    {
        string fileName = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + FileNameSuffix;
        return Path.Combine(directory, fileName);
    }
}
