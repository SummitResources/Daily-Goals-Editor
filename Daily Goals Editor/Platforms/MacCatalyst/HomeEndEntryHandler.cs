using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Daily_Goals_Editor;

public class HomeEndEntryHandler : EntryHandler
{
    protected override MauiTextField CreatePlatformView() => new HomeEndTextField { ClipsToBounds = true };
}

/// <summary>
/// Mac text fields follow the AppKit convention where Home and End scroll the document rather than move the cursor,
/// so in a single-line field they do nothing. Keyboards that have those keys expect them to jump to the start or end,
/// and to extend the selection there when Shift is held.
/// </summary>
public class HomeEndTextField : MauiTextField
{
    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (HomeOrEndKey(presses) is not { } key)
        {
            base.PressesBegan(presses, evt);
            return;
        }

        var isHome = key.KeyCode == UIKeyboardHidUsage.KeyboardHome;
        var target = isHome ? BeginningOfDocument : EndOfDocument;
        var anchor = target;
        if (key.ModifierFlags.HasFlag(UIKeyModifierFlags.Shift) && SelectedTextRange is { } selection)
        {
            anchor = isHome ? selection.End : selection.Start;
        }

        SelectedTextRange = isHome ? GetTextRange(target, anchor) : GetTextRange(anchor, target);
    }

    // UIKit requires the other press callbacks to be withheld from the base too when PressesBegan is.
    public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (HomeOrEndKey(presses) is null)
        {
            base.PressesEnded(presses, evt);
        }
    }

    public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        if (HomeOrEndKey(presses) is null)
        {
            base.PressesCancelled(presses, evt);
        }
    }

    private static UIKey? HomeOrEndKey(NSSet<UIPress> presses) => presses
        .ToArray()
        .Select(press => press.Key)
        .FirstOrDefault(key => key?.KeyCode is UIKeyboardHidUsage.KeyboardHome or UIKeyboardHidUsage.KeyboardEnd);
}
