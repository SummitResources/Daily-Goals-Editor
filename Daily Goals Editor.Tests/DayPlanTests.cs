using Daily_Goals_Editor.Core;

namespace Daily_Goals_Editor.Tests;

public class DayPlanTests
{
    // Deliberately not Product, so a test can tell a prefix-derived category apart from the fallback.
    private const Category DEFAULT_CATEGORY = Category.Operations;

    private static DayPlan PlanAfter(params string[] lines)
    {
        var plan = new DayPlan();
        foreach (var line in lines)
        {
            plan.Submit(line, DEFAULT_CATEGORY);
        }
        return plan;
    }

    [Fact]
    public void Submit_FreeFormLine_AddsTaskAndStaysAtMainPrompt()
    {
        var plan = PlanAfter("Reply to customer emails");

        var task = Assert.Single(plan.Tasks);
        Assert.Equal("Reply to customer emails", task.Title);
        Assert.Null(task.Ticket);
        Assert.Equal(DEFAULT_CATEGORY, task.Category);
        Assert.Null(plan.PromptTask);
        Assert.Equal("What else are you working on today?", plan.PromptQuestion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Submit_BlankLineAtMainPrompt_IsIgnored(string? line)
    {
        var plan = new DayPlan();

        var result = plan.Submit(line, DEFAULT_CATEGORY);

        Assert.Empty(plan.Tasks);
        Assert.Null(result.Message);
        Assert.Null(result.Task);
        Assert.Equal("What are you trying to accomplish today?", plan.PromptQuestion);
    }

    [Fact]
    public void Submit_TicketWithOnlyFiller_AsksForTitleThenSubItems()
    {
        var plan = PlanAfter("Work on PROD-1331");

        var task = Assert.Single(plan.Tasks);
        Assert.Null(task.Title);
        Assert.Equal("PROD-1331", task.Ticket);
        Assert.Same(task, plan.PromptTask);
        Assert.True(plan.AwaitingTitle);
        Assert.Equal("What is PROD-1331 about?", plan.PromptQuestion);

        plan.Submit("Checkout empty states", DEFAULT_CATEGORY);

        Assert.Equal("Checkout empty states", task.Title);
        Assert.False(plan.AwaitingTitle);
        Assert.Equal("What steps are involved in PROD-1331?", plan.PromptQuestion);
    }

    [Fact]
    public void Submit_BlankTitle_SkipsTitleAndKeepsPromptOpenForSubItems()
    {
        var plan = PlanAfter("PROD-1331", "");

        var task = Assert.Single(plan.Tasks);
        Assert.Null(task.Title);
        Assert.Same(task, plan.PromptTask);
        Assert.False(plan.AwaitingTitle);
    }

    [Fact]
    public void Submit_TicketWithTitleText_UsesItAsTitleAndSkipsTitleQuestion()
    {
        var plan = PlanAfter("ENG-441: resolve invoice export timeout");

        var task = Assert.Single(plan.Tasks);
        Assert.Equal("Resolve invoice export timeout", task.Title);
        Assert.Equal("ENG-441", task.Ticket);
        Assert.False(plan.AwaitingTitle);
        Assert.Equal("What steps are involved in ENG-441?", plan.PromptQuestion);
    }

    [Fact]
    public void Submit_LinesWhileTaskIsOpen_BecomeSubItemsUntilBlankLine()
    {
        var plan = PlanAfter("ENG-441 Export timeout", "Profile the export", "Add regression tests", "", "Reply to emails");

        Assert.Equal(2, plan.Tasks.Count);
        Assert.Equal(["Profile the export", "Add regression tests"], plan.Tasks[0].SubItems);
        Assert.Empty(plan.Tasks[1].SubItems);
        Assert.Null(plan.PromptTask);
    }

    [Fact]
    public void Submit_ExistingTicketWithOnlyFiller_ReopensWithoutAddingSubItem()
    {
        var plan = PlanAfter("PROD-1331 Checkout", "Write migration", "", "work on PROD-1331");

        var task = Assert.Single(plan.Tasks);
        Assert.Equal(["Write migration"], task.SubItems);
        Assert.Same(task, plan.PromptTask);
        Assert.Equal("Next step for PROD-1331?", plan.PromptQuestion);
    }

    [Fact]
    public void Submit_ExistingTicketWithText_AddsCapitalisedSubItemAndReopens()
    {
        var plan = PlanAfter("PROD-1331 Checkout", "", "prod-1331 deploy to staging");

        var task = Assert.Single(plan.Tasks);
        Assert.Equal(["Deploy to staging"], task.SubItems);
        Assert.Same(task, plan.PromptTask);
    }

    [Fact]
    public void Submit_SubItemForTaskWithBlankEditorRows_DropsTheBlankRows()
    {
        var plan = new DayPlan();
        var task = plan.AddUntitledTask(Category.Design);
        plan.SetTicket(task, "DES-12");

        plan.Submit("DES-12 sketch layouts", DEFAULT_CATEGORY);

        Assert.Equal(["Sketch layouts"], task.SubItems);
    }

    [Theory]
    [InlineData("PROD-1 Checkout", Category.Product)]
    [InlineData("ENG-2 Export", Category.Engineering)]
    [InlineData("DES-3 Layouts", Category.Design)]
    [InlineData("ACME-4 Unknown prefix", DEFAULT_CATEGORY)]
    public void Submit_NewTicket_TakesCategoryFromBuiltInPrefixOrFallsBack(string line, Category expected)
    {
        Assert.Equal(expected, PlanAfter(line).Tasks[0].Category);
    }

    [Fact]
    public void SetCategory_IsRememberedForLaterTicketsWithTheSamePrefix()
    {
        var plan = PlanAfter("PROD-1 Checkout", "");
        plan.SetCategory(plan.Tasks[0], Category.Design);

        plan.Submit("PROD-2 Pricing page", DEFAULT_CATEGORY);

        Assert.Equal(Category.Design, plan.Tasks[1].Category);
    }

    [Fact]
    public void SetCategory_OnFreeFormTask_DoesNotLearnAPrefix()
    {
        var plan = PlanAfter("Reply to emails");

        plan.SetCategory(plan.Tasks[0], Category.Engineering);

        Assert.Equal(Category.Engineering, plan.Tasks[0].Category);
        Assert.Empty(plan.PrefixCategories);
    }

    [Fact]
    public void SetTicket_UpperCasesAndLearnsPrefix()
    {
        var plan = PlanAfter("Reply to emails");

        plan.SetTicket(plan.Tasks[0], " sup-9 ");

        Assert.Equal("SUP-9", plan.Tasks[0].Ticket);
        Assert.Contains("SUP", plan.KnownPrefixes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void SetTicket_BlankValue_RemovesTicket(string? value)
    {
        var plan = PlanAfter("PROD-1 Checkout");

        plan.SetTicket(plan.Tasks[0], value);

        Assert.Null(plan.Tasks[0].Ticket);
    }

    [Fact]
    public void SetTicket_PartialValue_IsKeptButNotLearnedAsPrefix()
    {
        var plan = PlanAfter("Reply to emails");

        plan.SetTicket(plan.Tasks[0], "pro");

        Assert.Equal("PRO", plan.Tasks[0].Ticket);
        Assert.Empty(plan.KnownPrefixes);
    }

    [Fact]
    public void AddUntitledTask_ClosesThePromptAndAddsOneBlankEditorRow()
    {
        var plan = PlanAfter("PROD-1331");

        var task = plan.AddUntitledTask(Category.Engineering);

        Assert.Null(plan.PromptTask);
        Assert.False(plan.AwaitingTitle);
        Assert.Equal("Untitled task", task.Title);
        Assert.Equal(Category.Engineering, task.Category);
        Assert.Equal([""], task.SubItems);
        Assert.Equal(0, task.SubItemCount);
    }

    [Fact]
    public void Remove_TaskOpenInPrompt_ReturnsToMainPrompt()
    {
        var plan = PlanAfter("Reply to emails", "PROD-1331 Checkout");

        plan.Remove(plan.Tasks[1]);

        Assert.Single(plan.Tasks);
        Assert.Null(plan.PromptTask);
    }

    [Fact]
    public void Remove_OtherTask_KeepsPromptOpen()
    {
        var plan = PlanAfter("Reply to emails", "PROD-1331 Checkout");

        plan.Remove(plan.Tasks[0]);

        Assert.Same(plan.Tasks[0], plan.PromptTask);
    }

    [Fact]
    public void Move_SwapsWithNeighbour()
    {
        var plan = PlanAfter("First", "Second", "Third");
        var third = plan.Tasks[2];

        plan.Move(third, -1);

        Assert.Equal(["First", "Third", "Second"], plan.Tasks.Select(task => task.Title));
    }

    [Fact]
    public void StartNewDay_ClearsTasksAndPromptAndResetsDate()
    {
        var plan = PlanAfter("Reply to emails", "PROD-1331 Checkout");
        plan.Date = new DateOnly(2026, 1, 5);

        plan.StartNewDay();

        Assert.Empty(plan.Tasks);
        Assert.Null(plan.PromptTask);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), plan.Date);
    }

    [Fact]
    public void Format_NumbersTasksAndLettersNonBlankSubItems()
    {
        var plan = PlanAfter(
            "PROD-284 Finalize checkout", "Review copy", "Confirm mobile", "",
            "OPS-109", "", "",
            "Reply to emails");
        plan.Tasks[0].SubItems.Insert(1, " ");
        plan.Tasks[2].SubItems.Add("  ");

        Assert.Equal(
            "1. PROD-284 — Finalize checkout\n" +
            "   a. Review copy\n" +
            "   b. Confirm mobile\n" +
            "2. OPS-109\n" +
            "3. Reply to emails",
            plan.Format());
    }

    [Fact]
    public void Format_PastTwentySixSubItems_ContinuesWithDoubleLetters()
    {
        var plan = PlanAfter("Big task");
        plan.Tasks[0].SubItems.AddRange(Enumerable.Range(1, 28).Select(n => $"Step {n}"));

        var lines = plan.Format().Split('\n');

        Assert.Equal("   z. Step 26", lines[26]);
        Assert.Equal("   aa. Step 27", lines[27]);
        Assert.Equal("   ab. Step 28", lines[28]);
    }

    [Fact]
    public void Format_EmptyPlan_IsEmpty()
    {
        Assert.Equal("", new DayPlan().Format());
    }

    [Fact]
    public void ToExcelCell_QuotesAndDoublesEmbeddedQuotes()
    {
        Assert.Equal("\"1. Say \"\"hi\"\"\n   • now\"", DayPlan.ToExcelCell("1. Say \"hi\"\n   • now"));
    }
}
