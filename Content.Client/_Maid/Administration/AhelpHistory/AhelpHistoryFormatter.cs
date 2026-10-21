using System.Text;
using Content.Shared._Maid.Administration.AhelpHistory;
using Robust.Shared.Utility;

namespace Content.Client._Maid.Administration.AhelpHistory;

/// <summary>
/// Turns ahelp history entries into formatted text with hard line wrapping.
/// </summary>
public static class AhelpHistoryFormatter
{
    /// <summary>
    /// Maximum characters per line of message text; longer text continues on the next line.
    /// </summary>
    public const int LineWidth = 100;

    private const string ContinuationIndent = "      ";

    private static readonly Color HeaderColor = Color.FromHex("#a0a0a0");
    private static readonly Color AdminColor = Color.FromHex("#ff5555");
    private static readonly Color AdminOnlyColor = Color.FromHex("#ffaa00");

    public static string FormatHeader(int? roundId, string? serverName, DateTime startedAt, int messageCount)
    {
        var text = roundId is { } id
            ? Loc.GetString("ahelp-history-round-header",
                ("round", id.ToString()),
                ("server", serverName ?? "?"),
                ("date", ToLocal(startedAt).ToString("yyyy-MM-dd")),
                ("count", messageCount))
            : Loc.GetString("ahelp-history-no-round-header", ("count", messageCount));

        return $"--- {text} ---";
    }

    public static FormattedMessage FormatHeaderMessage(int? roundId, string? serverName, DateTime startedAt, int messageCount)
    {
        var msg = new FormattedMessage();
        msg.PushColor(HeaderColor);
        msg.AddText(FormatHeader(roundId, serverName, startedAt, messageCount));
        msg.Pop();
        return msg;
    }

    /// <summary>
    /// Appends one entry to <paramref name="msg"/>. Text is added as plain text, never parsed as markup.
    /// </summary>
    public static void AppendEntry(FormattedMessage msg, AhelpHistoryEntry entry)
    {
        msg.AddText($"[{ToLocal(entry.SentAt):HH:mm}] ");

        if (entry.AdminOnly)
        {
            msg.PushColor(AdminOnlyColor);
            msg.AddText(Loc.GetString("ahelp-history-admin-only-prefix") + " ");
            msg.Pop();
        }

        var sender = entry.SenderName;
        if (entry.FromWebhook)
            sender += " " + Loc.GetString("ahelp-history-discord-suffix");

        if (entry.SenderIsAdmin)
            msg.PushColor(AdminColor);

        msg.AddText(sender);

        if (entry.SenderIsAdmin)
            msg.Pop();

        msg.AddText(":\n" + Wrap(entry.Message));
    }

    public static FormattedMessage FormatEntry(AhelpHistoryEntry entry)
    {
        var msg = new FormattedMessage();
        AppendEntry(msg, entry);
        return msg;
    }

    /// <summary>
    /// Splits text into lines of at most <see cref="LineWidth"/> characters.
    /// Breaks at a space when there is one in the second half of the line, otherwise cuts mid-word.
    /// </summary>
    public static string Wrap(string text)
    {
        var sb = new StringBuilder(text.Length + text.Length / LineWidth * (ContinuationIndent.Length + 1));
        var first = true;

        foreach (var rawLine in text.Replace("\r", string.Empty).Split('\n'))
        {
            var line = rawLine;

            do
            {
                if (!first)
                    sb.Append('\n');
                first = false;
                sb.Append(ContinuationIndent);

                if (line.Length <= LineWidth)
                {
                    sb.Append(line);
                    break;
                }

                var cut = line.LastIndexOf(' ', LineWidth, LineWidth / 2 + 1);
                if (cut <= 0)
                    cut = LineWidth;

                sb.Append(line, 0, cut);
                line = line[cut..].TrimStart(' ');
            } while (line.Length > 0);
        }

        return sb.ToString();
    }

    public static DateTime ToLocal(DateTime utc)
    {
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
    }
}
