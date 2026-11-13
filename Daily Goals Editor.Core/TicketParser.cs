using System.Text.RegularExpressions;

namespace Daily_Goals_Editor.Core;

public static partial class TicketParser
{
    [GeneratedRegex(@"(?<![A-Za-z0-9])([A-Za-z][A-Za-z0-9]{1,9})-(\d+)(?![A-Za-z0-9])")]
    private static partial Regex TicketInTextRegex();

    [GeneratedRegex(@"^[A-Z][A-Z0-9]{1,9}-\d+$")]
    private static partial Regex WholeTicketRegex();

    /// <summary>
    /// Finds the first ticket key in free text, upper-cased. A lower-case key only counts once its prefix
    /// is known, so text like "utf-8" or "covid-19" isn't mistaken for a ticket.
    /// </summary>
    public static string? Find(string text, IReadOnlySet<string> knownPrefixes) =>
        TicketInTextRegex().Matches(text)
            .Select(match => (Prefix: match.Groups[1].Value, Number: match.Groups[2].Value))
            .Where(key => key.Prefix == key.Prefix.ToUpperInvariant() || knownPrefixes.Contains(key.Prefix.ToUpperInvariant()))
            .Select(key => $"{key.Prefix.ToUpperInvariant()}-{key.Number}")
            .FirstOrDefault();

    /// <summary>True when the whole value is one upper-case key, as typed into the Ticket ID field.</summary>
    public static bool IsTicket(string value) => WholeTicketRegex().IsMatch(value);

    public static string Prefix(string ticket) => ticket[..ticket.LastIndexOf('-')];

    /// <summary>Removes the ticket key from free text, matching it in any case.</summary>
    public static string Remove(string text, string ticket) =>
        Regex.Replace(text, $@"(?<![A-Za-z0-9]){Regex.Escape(ticket)}(?![A-Za-z0-9])", "", RegexOptions.IgnoreCase);
}
