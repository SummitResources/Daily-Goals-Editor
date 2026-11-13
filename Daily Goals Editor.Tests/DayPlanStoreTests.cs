using Daily_Goals_Editor.Core;

namespace Daily_Goals_Editor.Tests;

public sealed class DayPlanStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "daylist-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_NoSavedFile_ReturnsEmptyPlanForToday()
    {
        var plan = new DayPlanStore(_directory).Load();

        Assert.Empty(plan.Tasks);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), plan.Date);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsTasksAndLearnedPrefixes()
    {
        var store = new DayPlanStore(_directory);
        var plan = new DayPlan { Date = new DateOnly(2026, 11, 10) };
        plan.Submit("PROD-1331 Checkout", Category.Operations);
        plan.Submit("Write migration", Category.Operations);
        plan.Submit("", Category.Operations);
        plan.Submit("Reply to emails", Category.Design);
        plan.Tasks[1].Complete = true;
        plan.SetCategory(plan.Tasks[0], Category.Engineering);

        store.Save(plan);
        var loaded = store.Load();

        Assert.Equal(new DateOnly(2026, 11, 10), loaded.Date);
        Assert.Equal(plan.Format(), loaded.Format());
        Assert.Equal([Category.Engineering, Category.Design], loaded.Tasks.Select(task => task.Category));
        Assert.Equal([false, true], loaded.Tasks.Select(task => task.Complete));
        Assert.Equal(plan.Tasks.Select(task => task.Id), loaded.Tasks.Select(task => task.Id));
        Assert.Contains("PROD", loaded.KnownPrefixes);
        Assert.Equal(Category.Engineering, loaded.PrefixCategories["PROD"]);
        Assert.Null(loaded.PromptTask);
    }

    [Fact]
    public void Save_WritesCategoriesAsNames()
    {
        var plan = new DayPlan();
        plan.Submit("Reply to emails", Category.Design);

        new DayPlanStore(_directory).Save(plan);

        Assert.Contains("\"Category\": \"Design\"", File.ReadAllText(Path.Combine(_directory, "daily-goals.json")));
    }

    [Fact]
    public void Load_UnreadableFile_ThrowsInsteadOfStartingEmpty()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "daily-goals.json"), "{ not json");

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => new DayPlanStore(_directory).Load());
    }

    [Fact]
    public void Load_TaskMissingCategory_Throws()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "daily-goals.json"), """{ "Tasks": [ { "Title": "No category" } ] }""");

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => new DayPlanStore(_directory).Load());
    }

    [Fact]
    public void Load_JsonNull_Throws()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "daily-goals.json"), "null");

        Assert.Throws<InvalidDataException>(() => new DayPlanStore(_directory).Load());
    }
}
