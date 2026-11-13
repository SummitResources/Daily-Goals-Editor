using System.Globalization;
using Daily_Goals_Editor.Core;
using Daily_Goals_Editor.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;

namespace Daily_Goals_Editor;

public partial class MainPage : ContentPage
{
    private readonly DayPlanStore _store;
    private readonly ILogger<MainPage> _logger;
    private readonly DayPlan _plan;
    private readonly List<Entry> _subItemEntries = [];

    private Guid? _selectedTaskId;
    private Category? _categoryFilter;
    private CancellationTokenSource? _copiedResetCancellation;
    private Entry? _titleEntry;

    public MainPage(DayPlanStore store, ILogger<MainPage> logger)
    {
        InitializeComponent();

        _store = store;
        _logger = logger;
        _plan = store.Load();
        _selectedTaskId = _plan.Tasks.FirstOrDefault()?.Id;

        DateLabel.Text = DateTime.Today.ToString("dddd, MMMM d", CultureInfo.CurrentCulture);
        if (_plan.Tasks.Count > 0 && _plan.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            ShowStatus($"Picked up your list from {_plan.Date:dddd, MMM d}. Use \"Start a new day\" to clear it.");
        }

        ProgressTrack.SizeChanged += (_, _) => RefreshProgress();
        RefreshTextScaleControls();
        Refresh(rebuildEditor: true);
    }

    private DayTask? SelectedTask => _plan.Tasks.FirstOrDefault(task => task.Id == _selectedTaskId);

    private Category NewTaskCategory => _categoryFilter ?? Category.Product;

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if MACCATALYST
        // The design draws its own title bar, so the system one would show the app name twice.
        if (Window?.Handler?.PlatformView is UIKit.UIWindow { WindowScene.Titlebar: { } titlebar })
        {
            titlebar.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
            titlebar.Toolbar = null;
        }
#endif
        FocusPrompt();
    }

    private void OnSubmitPrompt(object? sender, EventArgs e)
    {
        var result = _plan.Submit(PromptEntry.Text, NewTaskCategory);
        PromptEntry.Text = null;
        ShowStatus(result.Message);

        if (result.Task is not null)
        {
            _selectedTaskId = result.Task.Id;
            if (_categoryFilter is not null && result.Task.Category != _categoryFilter)
            {
                _categoryFilter = null;
            }
        }

        SaveAndRefresh(rebuildEditor: true);
        FocusPrompt();
    }

    private void OnDone(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PromptEntry.Text))
        {
            _plan.Submit(PromptEntry.Text, NewTaskCategory);
        }
        PromptEntry.Text = null;
        _plan.ReturnToMainPrompt();
        ShowStatus(null);
        SaveAndRefresh(rebuildEditor: true);
        FocusPrompt();
    }

    private void OnAddTask(object? sender, EventArgs e)
    {
        var task = _plan.AddUntitledTask(NewTaskCategory);
        _selectedTaskId = task.Id;
        SaveAndRefresh(rebuildEditor: true);

        Dispatcher.Dispatch(() =>
        {
            if (_titleEntry is null)
            {
                return;
            }
            _titleEntry.Focus();
            _titleEntry.CursorPosition = 0;
            _titleEntry.SelectionLength = _titleEntry.Text?.Length ?? 0;
        });
    }

    private async void OnCopy(object? sender, EventArgs e)
    {
        const int COPIED_CONFIRMATION_MILLISECONDS = 1800; // how long the design shows the "Copied" state

        if (_plan.Tasks.Count == 0)
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(DayPlan.ToExcelCell(_plan.Format()));
        CopyButton.Text = "Copied";
        CopyButton.Icon = IconKind.Check;
        CopyButton.Variant = ButtonVariant.Success;

        _copiedResetCancellation?.Cancel();
        _copiedResetCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(COPIED_CONFIRMATION_MILLISECONDS, _copiedResetCancellation.Token);
            CopyButton.Text = "Copy";
            CopyButton.Icon = IconKind.Copy;
            CopyButton.Variant = ButtonVariant.Secondary;
        }
        catch (TaskCanceledException)
        {
            // A newer copy restarted the confirmation; that one will reset the button.
        }
    }

    private async void OnStartNewDay(object? sender, EventArgs e)
    {
        if (_plan.Tasks.Count > 0 &&
            !await DisplayAlertAsync("Start a new day?", "This clears the current list.", "Clear", "Cancel"))
        {
            return;
        }

        _plan.StartNewDay();
        _selectedTaskId = null;
        _categoryFilter = null;
        ShowStatus(null);
        SaveAndRefresh(rebuildEditor: true);
        FocusPrompt();
    }

    private void OnSmallerText(object? sender, EventArgs e) => SetTextScale(TextScale.Current - TextScale.STEP);

    private void OnLargerText(object? sender, EventArgs e) => SetTextScale(TextScale.Current + TextScale.STEP);

    private void OnResetTextScale(object? sender, EventArgs e) => SetTextScale(1);

    private void SetTextScale(double scale)
    {
        TextScale.Set(scale);
        RefreshTextScaleControls();
    }

    private void RefreshTextScaleControls()
    {
        TextScaleButton.Text = TextScale.Current.ToString("P0", CultureInfo.CurrentCulture);
        SmallerTextButton.IsEnabled = TextScale.Current > TextScale.MINIMUM;
        LargerTextButton.IsEnabled = TextScale.Current < TextScale.MAXIMUM;
    }

    private void ShowStatus(string? message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = message is not null;
    }

    private void FocusPrompt() => Dispatcher.Dispatch(() => PromptEntry.Focus());

    /// <summary>The editor is only rebuilt for structural changes, so typing in its fields keeps focus.</summary>
    private void SaveAndRefresh(bool rebuildEditor)
    {
        try
        {
            _store.Save(_plan);
            SaveStateLabel.Text = "Saved locally";
            SaveStateDot.Fill = new SolidColorBrush(Ui.ColorResource("Success"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to save the day plan");
            SaveStateLabel.Text = "Not saved";
            SaveStateDot.Fill = new SolidColorBrush(Ui.ColorResource("Danger"));
        }

        Refresh(rebuildEditor);
    }

    private void Refresh(bool rebuildEditor)
    {
        PromptLabel.Text = _plan.PromptQuestion;
        HintLabel.Text = _plan.PromptHint;
        DoneButton.IsVisible = _plan.PromptTask is not null;
        DoneButton.Text = _plan.AwaitingTitle ? "Skip" : "Done";
        ColumnTitle.Text = _categoryFilter?.ToString() ?? "Today’s tasks";

        var output = _plan.Format();
        if (output.Length == 0)
        {
            OutputLabel.Text = "Add a task to generate your list.";
            OutputLabel.TextColor = Ui.ColorResource("Subtle");
        }
        else
        {
            OutputLabel.Text = output;
            OutputLabel.TextColor = Ui.ColorResource("Ink");
        }

        CategoryNav.Children.Clear();
        CategoryNav.Children.Add(NavRow(null, "All tasks", _plan.Tasks.Count));
        foreach (var category in Enum.GetValues<Category>())
        {
            CategoryNav.Children.Add(NavRow(category, category.ToString(), _plan.Tasks.Count(task => task.Category == category)));
        }

        TaskList.Children.Clear();
        var visibleTasks = _categoryFilter is null
            ? _plan.Tasks
            : _plan.Tasks.Where(task => task.Category == _categoryFilter).ToList();
        if (visibleTasks.Count == 0)
        {
            TaskList.Children.Add(EmptyTaskList());
        }
        else
        {
            foreach (var task in visibleTasks)
            {
                TaskList.Children.Add(TaskCard(task));
            }
        }

        RefreshProgress();
        if (rebuildEditor)
        {
            BuildEditor();
        }
    }

    private void RefreshProgress()
    {
        var total = _plan.Tasks.Count;
        var complete = _plan.Tasks.Count(task => task.Complete);
        ProgressLabel.Text = $"{complete}/{total}";
        ProgressFill.WidthRequest = total == 0 || ProgressTrack.Width <= 0 ? 0 : ProgressTrack.Width * complete / total;
    }

    /// <param name="category">The category to filter by, or null for "All tasks".</param>
    private Border NavRow(Category? category, string name, int count)
    {
        var isActive = _categoryFilter == category;

        View marker = category is null
            ? new Icon(IconKind.List, 15, Ui.ColorResource(isActive ? "Accent" : "Muted")) { VerticalOptions = LayoutOptions.Center }
            : Ui.CategoryDot(category.Value, 9);
        marker.Margin = category is null ? 0 : new Thickness(3, 0);

        var content = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 10
        };
        content.Add(marker, 0);
        content.Add(new Label
        {
            Text = name,
            VerticalOptions = LayoutOptions.Center,
            FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None,
            TextColor = Ui.ColorResource(isActive ? "Ink" : "InkSoft")
        }.WithTextSize(TextSize.Medium), 1);
        content.Add(new Label { Text = count.ToString(), Style = Ui.StyleResource("Small"), VerticalOptions = LayoutOptions.Center }, 2);

        var row = new Border
        {
            Content = content,
            Padding = new Thickness(10, 8),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 }
        };
        if (isActive)
        {
            row.BackgroundColor = Ui.ColorResource("Surface");
            row.Stroke = Ui.ColorResource("Line");
        }
        else
        {
            row.Stroke = Colors.Transparent;
            row.Hover(Colors.Transparent, Ui.ColorResource("Hover"));
        }

        return row.OnTap(() =>
        {
            _categoryFilter = category;
            Refresh(rebuildEditor: false);
        });
    }

    private Border TaskCard(DayTask task)
    {
        var isSelected = task.Id == _selectedTaskId;

        var completeCheck = new Border
        {
            WidthRequest = 20,
            HeightRequest = 20,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            StrokeThickness = 1.5,
            Stroke = Ui.ColorResource(task.Complete ? "Success" : "LineStrong"),
            BackgroundColor = Ui.ColorResource(task.Complete ? "Success" : "Surface"),
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 1, 0, 0),
            Content = task.Complete
                ? new Icon(IconKind.Check, 13, Colors.White) { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                : null
        }.OnTap(() =>
        {
            task.Complete = !task.Complete;
            SaveAndRefresh(rebuildEditor: false);
        });

        var title = new Label
        {
            Text = task.DisplayTitle,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Ui.ColorResource(task.Complete ? "Subtle" : "Ink"),
            TextDecorations = task.Complete ? TextDecorations.Strikethrough : TextDecorations.None
        };

        var meta = new HorizontalStackLayout { Spacing = 6 };
        if (task.Ticket is not null)
        {
            meta.Add(new Border
            {
                BackgroundColor = Ui.ColorResource("Chip"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Padding = new Thickness(6, 1),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label { Text = task.Ticket, Style = Ui.StyleResource("Mono"), TextColor = Ui.ColorResource("InkSoft") }.WithTextSize(TextSize.ExtraSmall)
            });
            meta.Add(MetaSeparator());
        }
        meta.Add(Ui.CategoryDot(task.Category, 7));
        meta.Add(new Label { Text = task.Category.ToString(), Style = Ui.StyleResource("Small"), VerticalOptions = LayoutOptions.Center });
        meta.Add(MetaSeparator());
        meta.Add(new Label
        {
            Text = task.SubItemCount == 1 ? "1 sub-item" : $"{task.SubItemCount} sub-items",
            Style = Ui.StyleResource("Small"),
            VerticalOptions = LayoutOptions.Center
        });

        var content = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 12
        };
        content.Add(completeCheck, 0);
        content.Add(new VerticalStackLayout { Spacing = 5, Children = { title, meta } }, 1);
        if (_plan.PromptTask == task)
        {
            content.Add(new Border
            {
                BackgroundColor = Ui.ColorResource("AccentTint"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(8, 2),
                VerticalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = _plan.AwaitingTitle ? "Adding title" : "Adding steps",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.ColorResource("Accent")
                }.WithTextSize(TextSize.ExtraSmall)
            }, 2);
        }

        var card = new Border
        {
            Content = content,
            Padding = new Thickness(16, 14),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            StrokeThickness = isSelected ? 1.5 : 1
        };
        if (isSelected)
        {
            card.BackgroundColor = Ui.ColorResource("Surface");
            card.Stroke = Ui.ColorResource("Accent");
        }
        else
        {
            card.Hover(Ui.ColorResource("Surface"), Ui.ColorResource("Surface"), Ui.ColorResource("Line"), Ui.ColorResource("LineStrong"));
        }

        return card.OnTap(() =>
        {
            if (_selectedTaskId == task.Id)
            {
                return;
            }
            _selectedTaskId = task.Id;
            Refresh(rebuildEditor: true);
        });
    }

    private static Label MetaSeparator() => new()
    {
        Text = "·",
        Style = Ui.StyleResource("Small"),
        TextColor = Ui.ColorResource("Subtle"),
        VerticalOptions = LayoutOptions.Center
    };

    private Border EmptyTaskList() => new()
    {
        Style = Ui.StyleResource("Card"),
        StrokeDashArray = [4, 3],
        Stroke = Ui.ColorResource("LineStrong"),
        BackgroundColor = Colors.Transparent,
        Padding = new Thickness(24, 32),
        Content = new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                IconBadge(IconKind.List, 21, 42),
                new Label
                {
                    Text = _categoryFilter is null
                        ? "No tasks yet. Answer the prompt above to get started."
                        : $"No tasks in {_categoryFilter} yet.",
                    Style = Ui.StyleResource("Small"),
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new UiButton("Add the first one", IconKind.None, ButtonVariant.Ghost, () => OnAddTask(null, EventArgs.Empty), compact: true)
                {
                    HorizontalOptions = LayoutOptions.Center
                }
            }
        }
    };

    private static Border IconBadge(IconKind kind, double iconSize, double badgeSize) => new()
    {
        WidthRequest = badgeSize,
        HeightRequest = badgeSize,
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = badgeSize / 2 },
        BackgroundColor = Ui.ColorResource("AccentTint"),
        HorizontalOptions = LayoutOptions.Center,
        Content = new Icon(kind, iconSize, Ui.ColorResource("Accent")) { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
    };

    private void BuildEditor()
    {
        EditorHost.Children.Clear();
        _subItemEntries.Clear();
        _titleEntry = null;

        if (SelectedTask is not { } task)
        {
            EditorHost.Children.Add(new VerticalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(0, 60, 0, 0),
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    IconBadge(IconKind.List, 24, 52),
                    new Label { Text = "Select a task to edit", FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center }.WithTextSize(TextSize.Large),
                    new Label { Text = "Choose a task from the list or create a new one.", Style = Ui.StyleResource("Small"), HorizontalTextAlignment = TextAlignment.Center },
                    new UiButton("Add task", IconKind.Plus, ButtonVariant.Primary, () => OnAddTask(null, EventArgs.Empty))
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 6, 0, 0)
                    }
                }
            });
            return;
        }

        var index = _plan.Tasks.IndexOf(task);
        var toolbarActions = new HorizontalStackLayout { Spacing = 2 };
        toolbarActions.Add(new UiButton("", IconKind.ChevronUp, ButtonVariant.Ghost, () =>
        {
            _plan.Move(task, -1);
            SaveAndRefresh(rebuildEditor: true);
        }) { IsEnabled = index > 0 });
        toolbarActions.Add(new UiButton("", IconKind.ChevronDown, ButtonVariant.Ghost, () =>
        {
            _plan.Move(task, 1);
            SaveAndRefresh(rebuildEditor: true);
        }) { IsEnabled = index < _plan.Tasks.Count - 1 });
        toolbarActions.Add(new BoxView { WidthRequest = 1, HeightRequest = 18, Color = Ui.ColorResource("Line"), Margin = new Thickness(6, 0), VerticalOptions = LayoutOptions.Center });
        toolbarActions.Add(new UiButton("", IconKind.Trash, ButtonVariant.Danger, () =>
        {
            _plan.Remove(task);
            _selectedTaskId = _plan.Tasks.Count == 0 ? null : _plan.Tasks[Math.Min(index, _plan.Tasks.Count - 1)].Id;
            SaveAndRefresh(rebuildEditor: true);
        }));

        var toolbar = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        toolbar.Add(new Label { Text = "Edit task", Style = Ui.StyleResource("SectionTitle"), VerticalOptions = LayoutOptions.Center }, 0);
        toolbar.Add(toolbarActions, 1);
        EditorHost.Children.Add(toolbar);

        var titleEntry = new Entry { Text = task.Title, Placeholder = task.Ticket ?? "What needs doing?" };
        titleEntry.TextChanged += (_, e) =>
        {
            task.Title = string.IsNullOrWhiteSpace(e.NewTextValue) ? null : e.NewTextValue;
            SaveAndRefresh(rebuildEditor: false);
        };
        _titleEntry = titleEntry;
        EditorHost.Children.Add(Field("Task title", Ui.Input(titleEntry)));

        var ticketEntry = new Entry { Text = task.Ticket, Placeholder = "e.g. PROD-123" };
        ticketEntry.TextChanged += (_, e) =>
        {
            var upperCased = e.NewTextValue?.ToUpperInvariant();
            if (upperCased != e.NewTextValue)
            {
                // Setting Text raises TextChanged again with the upper-cased value, which then saves it.
                ticketEntry.Text = upperCased;
                return;
            }
            _plan.SetTicket(task, upperCased);
            SaveAndRefresh(rebuildEditor: false);
        };

        var categorySelect = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 8
        };
        categorySelect.Add(Ui.CategoryDot(task.Category, 8), 0);
        categorySelect.Add(new Label { Text = task.Category.ToString(), VerticalOptions = LayoutOptions.Center }, 1);
        categorySelect.Add(new Icon(IconKind.ChevronDown, 15, Ui.ColorResource("Muted")) { VerticalOptions = LayoutOptions.Center }, 2);
        var categoryField = Ui.Input(categorySelect).OnTap(async () =>
        {
            var choice = await DisplayActionSheetAsync("Category", "Cancel", null, Enum.GetNames<Category>());
            if (Enum.TryParse<Category>(choice, out var category))
            {
                _plan.SetCategory(task, category);
                SaveAndRefresh(rebuildEditor: true);
            }
        });

        var ticketAndCategory = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 12 };
        ticketAndCategory.Add(Field("Ticket ID", Ui.Input(ticketEntry)), 0);
        ticketAndCategory.Add(Field("Category", categoryField), 1);
        EditorHost.Children.Add(ticketAndCategory);

        var subItemsHeading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        subItemsHeading.Add(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "Sub-items", Style = Ui.StyleResource("SectionTitle") },
                new Label { Text = "Add one detail per line", Style = Ui.StyleResource("Small") }
            }
        }, 0);
        subItemsHeading.Add(new UiButton("Add item", IconKind.Plus, ButtonVariant.Secondary, () => AddSubItem(task), compact: true)
        {
            VerticalOptions = LayoutOptions.Center
        }, 1);

        var subItemRows = new VerticalStackLayout { Spacing = 8 };
        for (var subItemIndex = 0; subItemIndex < task.SubItems.Count; subItemIndex++)
        {
            subItemRows.Add(SubItemRow(task, subItemIndex));
        }

        EditorHost.Children.Add(new VerticalStackLayout { Spacing = 12, Children = { subItemsHeading, subItemRows } });
    }

    private Grid SubItemRow(DayTask task, int subItemIndex)
    {
        var entry = new Entry
        {
            Text = task.SubItems[subItemIndex],
            Placeholder = "Describe the next step",
            ReturnType = ReturnType.Next
        };
        entry.TextChanged += (_, e) =>
        {
            task.SubItems[subItemIndex] = e.NewTextValue;
            SaveAndRefresh(rebuildEditor: false);
        };
        entry.Completed += (_, _) =>
        {
            if (subItemIndex == task.SubItems.Count - 1)
            {
                AddSubItem(task);
            }
            else
            {
                _subItemEntries[subItemIndex + 1].Focus();
            }
        };
        _subItemEntries.Add(entry);

        var row = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 8
        };
        row.Add(new Label { Text = "⋮⋮", TextColor = Ui.ColorResource("Subtle"), VerticalOptions = LayoutOptions.Center }.WithTextSize(TextSize.Small), 0);
        row.Add(Ui.Input(entry), 1);
        row.Add(new UiButton("", IconKind.Trash, ButtonVariant.Danger, () =>
        {
            task.SubItems.RemoveAt(subItemIndex);
            SaveAndRefresh(rebuildEditor: true);
        }) { VerticalOptions = LayoutOptions.Center }, 2);
        return row;
    }

    private void AddSubItem(DayTask task)
    {
        task.SubItems.Add("");
        SaveAndRefresh(rebuildEditor: true);
        Dispatcher.Dispatch(() => _subItemEntries[^1].Focus());
    }

    private static VerticalStackLayout Field(string label, View input) => new()
    {
        Spacing = 6,
        Children = { new Label { Text = label, Style = Ui.StyleResource("FieldLabel") }, input }
    };
}
