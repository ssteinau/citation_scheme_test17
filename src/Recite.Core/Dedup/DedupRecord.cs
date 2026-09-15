using Recite.Core.Model;

namespace Recite.Core.Dedup;

/// <summary>
/// A lightweight, framework-free projection of an item used for duplicate detection.
/// The Data layer builds these from loaded items so the matcher stays pure and testable.
/// </summary>
public sealed record DedupRecord(
    Guid Uuid,
    string Type,
    string? Title,
    int? Year,
    IReadOnlyList<string> AuthorKeys,
    IReadOnlyList<(string Scheme, string Value)> Identifiers)
{
    public string NormalizedTitle { get; } = TextNormalizer.Fold(Title);

    /// <summary>Blocking key to avoid O(n²): first significant title word + year + type.</summary>
    public string BlockKey =>
        $"{TextNormalizer.FirstSignificantWord(Title)}|{Year?.ToString() ?? "?"}|{Type}";

    public static DedupRecord From(Item item)
    {
        var authorKeys = item.Contributions
            .Where(c => c.Role is ContributorRole.Author or ContributorRole.Editor)
            .OrderBy(c => c.Order)
            .Select(c => c.Person is not null
                ? Person.ComputeKey(c.Person)
                : "")
            .Where(s => s.Length > 0)
            .ToList();

        var ids = item.Identifiers
            .Select(i => (i.Scheme, ExternalIdentifier.Normalize(i.Scheme, i.Value)))
            .ToList();

        return new DedupRecord(item.Uuid, item.Type, item.Title, item.Year, authorKeys, ids);
    }
}
