using System.Text.Json.Serialization;

namespace Daily_Goals_Editor.Core;

/// <param name="Message">Feedback to show under the prompt, or null when there is nothing to say.</param>
/// <param name="Task">The task the line was applied to, or null when the line was ignored.</param>
public sealed record SubmitResult(string? Message, DayTask? Task);

/// <summary>
/// The day's task list and the guided prompt that fills it. At the main prompt, a line mentioning a ticket
/// starts or reopens that ticket's task, first asking for a title when the line was only filler like
/// "work on PROD-1331"; any other line is a free-form task. While a task is open in the prompt, each line
/// becomes one of its sub-items, and an empty line returns to the main prompt.
/// </summary>
public sealed class DayPlan
{
    private static readonly HashSet<string> FILLER_WORDS = new(StringComparer.OrdinalIgnoreCase)
    {
        "work", "working", "on", "for", "continue", "continuing", "finish", "finishing", "start",
        "starting", "the", "ticket", "back", "to", "more", "keep", "resume", "with", "and"
    };

    private static readonly Dictionary<string, Category> BUILT_IN_PREFIX_CATEGORIES = new()
    {
        ["PROD"] = Category.Product, ["PRD"] = Category.Product, ["PM"] = Category.Product,
        ["ENG"] = Category.Engineering, ["DEV"] = Category.Engineering, ["BUG"] = Category.Engineering,
        ["API"] = Category.Engineering, ["TECH"] = Category.Engineering,
        ["DES"] = Category.Design, ["DSN"] = Category.Design, ["UX"] = Category.Design, ["UI"] = Category.Design,
        ["OPS"] = Category.Operations, ["OP"] = Category.Operations, ["IT"] = Category.Operations,
        ["SUP"] = Category.Operations
    };

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public List<DayTask> Tasks { get; init; } = [];

    /// <summary>Ticket prefixes used so far, so a lower-case "prod-12" is recognised once PROD has been seen.</summary>
    public HashSet<string> KnownPrefixes { get; init; } = [];

    /// <summary>The category last chosen for each ticket prefix; it wins over the built-in guesses.</summary>
    public Dictionary<string, Category> PrefixCategories { get; init; } = [];

    [JsonIgnore]
    public DayTask? PromptTask { get; private set; }

    [JsonIgnore]
    public bool AwaitingTitle { get; private set; }

    [JsonIgnore]
    public string PromptQuestion
    {
        get
        {
            if (PromptTask is null)
            {
                return Tasks.Count == 0
                    ? "What are you trying to accomplish today?"
                    : "What else are you working on today?";
            }

            if (AwaitingTitle)
            {
                return $"What is {PromptTask.PromptName} about?";
            }

            return PromptTask.SubItemCount == 0
                ? $"What steps are involved in {PromptTask.PromptName}?"
                : $"Next step for {PromptTask.PromptName}?";
        }
    }

    [JsonIgnore]
    public string PromptHint
    {
        get
        {
            if (PromptTask is null)
            {
                return "Mention a ticket (e.g. \"Work on PROD-1331\") to group steps under it, or type anything free-form.";
            }

            return AwaitingTitle
                ? "A short title for the ticket. Press Enter to skip."
                : "One step per line. Press Enter on an empty line (or Done) when finished.";
        }
    }

    /// <param name="input">The line typed at the prompt.</param>
    /// <param name="defaultCategory">Category for a new task whose ticket prefix doesn't suggest one.</param>
    public SubmitResult Submit(string? input, Category defaultCategory)
    {
        var text = input?.Trim();

        if (PromptTask is not null && AwaitingTitle)
        {
            AwaitingTitle = false;
            if (string.IsNullOrEmpty(text))
            {
                return new SubmitResult(null, PromptTask);
            }

            PromptTask.Title = text;
            return new SubmitResult($"Titled {PromptTask.PromptName}.", PromptTask);
        }

        if (PromptTask is not null)
        {
            var openTask = PromptTask;
            if (string.IsNullOrEmpty(text))
            {
                ReturnToMainPrompt();
                return new SubmitResult($"Finished {openTask.PromptName}.", openTask);
            }

            AppendSubItem(openTask, text);
            return new SubmitResult($"Added step to {openTask.PromptName}.", openTask);
        }

        if (string.IsNullOrEmpty(text))
        {
            return new SubmitResult(null, null);
        }

        var ticket = TicketParser.Find(text, KnownPrefixes);
        if (ticket is null)
        {
            var freeFormTask = new DayTask { Title = text, Category = defaultCategory };
            Tasks.Add(freeFormTask);
            return new SubmitResult("Added.", freeFormTask);
        }

        var prefix = TicketParser.Prefix(ticket);
        KnownPrefixes.Add(prefix);

        var remainder = TicketParser.Remove(text, ticket).Trim().Trim(':', '-', ',', '–', '—').Trim();
        var remainderIsFiller = remainder
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Trim(':', '-', ',', '.', '–', '—'))
            .Where(word => word.Length > 0)
            .All(FILLER_WORDS.Contains);
        var title = remainderIsFiller ? null : char.ToUpperInvariant(remainder[0]) + remainder[1..];

        var existingTask = Tasks.FirstOrDefault(task => task.Ticket == ticket);
        if (existingTask is not null)
        {
            PromptTask = existingTask;
            AwaitingTitle = false;
            if (title is null)
            {
                return new SubmitResult($"{ticket} is already on your list. Adding more steps.", existingTask);
            }

            AppendSubItem(existingTask, title);
            return new SubmitResult($"Added \"{title}\" under {ticket}.", existingTask);
        }

        var ticketTask = new DayTask
        {
            Title = title,
            Ticket = ticket,
            Category = PrefixCategories.TryGetValue(prefix, out var learnedCategory)
                ? learnedCategory
                : BUILT_IN_PREFIX_CATEGORIES.GetValueOrDefault(prefix, defaultCategory)
        };
        Tasks.Add(ticketTask);
        PromptTask = ticketTask;
        AwaitingTitle = title is null;
        return new SubmitResult($"Started {ticket}.", ticketTask);
    }

    /// <summary>Adds a placeholder task for filling in through the edit form rather than the prompt.</summary>
    public DayTask AddUntitledTask(Category category)
    {
        ReturnToMainPrompt();
        var task = new DayTask { Title = "Untitled task", Category = category, SubItems = [""] };
        Tasks.Add(task);
        return task;
    }

    public void ReturnToMainPrompt()
    {
        PromptTask = null;
        AwaitingTitle = false;
    }

    public void Remove(DayTask task)
    {
        Tasks.Remove(task);
        if (PromptTask == task)
        {
            ReturnToMainPrompt();
        }
    }

    public void Move(DayTask task, int offset)
    {
        var index = Tasks.IndexOf(task);
        (Tasks[index], Tasks[index + offset]) = (Tasks[index + offset], Tasks[index]);
    }

    /// <summary>Upper-cases the Ticket ID field's value; a blank value removes the ticket.</summary>
    public void SetTicket(DayTask task, string? value)
    {
        var ticket = value?.Trim().ToUpperInvariant();
        task.Ticket = string.IsNullOrEmpty(ticket) ? null : ticket;
        if (task.Ticket is not null && TicketParser.IsTicket(task.Ticket))
        {
            KnownPrefixes.Add(TicketParser.Prefix(task.Ticket));
        }
    }

    /// <summary>Also remembers the choice for the ticket's prefix, so later tickets like it land in the same category.</summary>
    public void SetCategory(DayTask task, Category category)
    {
        task.Category = category;
        if (task.Ticket is not null && TicketParser.IsTicket(task.Ticket))
        {
            PrefixCategories[TicketParser.Prefix(task.Ticket)] = category;
        }
    }

    public void StartNewDay()
    {
        Tasks.Clear();
        ReturnToMainPrompt();
        Date = DateOnly.FromDateTime(DateTime.Today);
    }

    /// <summary>
    /// The numbered list with lettered sub-items, joined with \n, e.g.
    /// <code>
    /// 1. PROD-284 — Finalize checkout empty states
    ///    a. Review updated copy with the content team
    ///    b. Confirm mobile layouts
    /// </code>
    /// </summary>
    public string Format() =>
        string.Join('\n', Tasks.SelectMany((task, index) =>
            task.SubItems
                .Where(subItem => !string.IsNullOrWhiteSpace(subItem))
                .Select((subItem, subIndex) => $"   {SubItemLetter(subIndex)}. {subItem.Trim()}")
                .Prepend(task switch
                {
                    { Ticket: { } ticket, Title: { } title } => $"{index + 1}. {ticket} — {title}",
                    { Ticket: { } ticket } => $"{index + 1}. {ticket}",
                    _ => $"{index + 1}. {task.DisplayTitle}"
                })));

    /// <summary>
    /// Wraps text as one quoted field, which Excel pastes into the selected cell with its line breaks
    /// instead of spreading the lines down the rows.
    /// </summary>
    public static string ToExcelCell(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";

    /// <summary>a, b, … z, then aa, ab, … like spreadsheet columns, so a long list never runs out of letters.</summary>
    private static string SubItemLetter(int index)
    {
        var letters = "";
        for (var remaining = index + 1; remaining > 0; remaining = (remaining - 1) / 26)
        {
            letters = (char)('a' + (remaining - 1) % 26) + letters;
        }
        return letters;
    }

    private static void AppendSubItem(DayTask task, string subItem)
    {
        // Blank rows are empty editor placeholders; without this the new sub-item would land after a gap.
        task.SubItems.RemoveAll(string.IsNullOrWhiteSpace);
        task.SubItems.Add(subItem);
    }
}
