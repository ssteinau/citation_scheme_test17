namespace Recite.Core.Model;

/// <summary>A publication series (e.g. LNCS) referenced by containers (PLAN §3).</summary>
public class Series
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string? Abbreviation { get; set; }
    public string? Issn { get; set; }

    public string UniqueKey { get; set; } = "";

    public List<Item> Items { get; set; } = new();

    public static string ComputeKey(string name) => TextNormalizer.Fold(name);
}
