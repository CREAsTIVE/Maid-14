using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server._Maid.Toolshed.Commands;

public record struct NotMutableCollectionError : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("The collection is not mutable (read-only or does not support Add/Remove).");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct IndexOutOfBoundsError(int Index, int Count) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Index {Index} is out of bounds (Count: {Count}).");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
