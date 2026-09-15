namespace Recite.Core.Model;

/// <summary>
/// A lightweight shared journal entity (PLAN §3). Articles reference it by
/// <see cref="Item.JournalId"/>; its canonical <see cref="Abbreviation"/> is the
/// default output short form, overridable per project via a projection.
/// </summary>
public class Journal
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string? Abbreviation { get; set; }
    public string? Issn { get; set; }
    public string? Eissn { get; set; }

    /// <summary>Normalized dedup key (folded name); unique index in the DB.</summary>
    public string UniqueKey { get; set; } = "";

    public List<Item> Items { get; set; } = new();

    public static string ComputeKey(string name) => TextNormalizer.Fold(name);
}
