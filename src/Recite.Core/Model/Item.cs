using System.Text.Json.Nodes;
using Recite.Core.Csl;

namespace Recite.Core.Model;

/// <summary>
/// A reference. Its identity is the immutable <see cref="Uuid"/> assigned at creation
/// (PLAN §3). Non-factored CSL fields live in <see cref="FieldsJson"/> (a queryable JSON
/// column); contributors, journal, series and container are factored into relations.
/// </summary>
public class Item
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = CslType.Document;

    /// <summary>CSL field bag (title, issued, volume, issue, page, abstract…) as a JSON object.</summary>
    public string FieldsJson { get; set; } = "{}";

    /// <summary>Denormalised for list display / sorting / FTS; kept in sync on save.</summary>
    public string? Title { get; set; }
    public int? Year { get; set; }

    /// <summary>Recite-generated default citation key (imported keys are not trusted, PLAN §7).</summary>
    public string? CitationKey { get; set; }

    public DateTimeOffset DateAdded { get; set; }
    public DateTimeOffset DateModified { get; set; }

    // Factored-out relations --------------------------------------------------
    public int? JournalId { get; set; }
    public Journal? Journal { get; set; }

    public int? SeriesId { get; set; }
    public Series? Series { get; set; }

    /// <summary>crossref parent: the proceedings volume / edited book this contribution is in.</summary>
    public int? ContainerParentId { get; set; }
    public Item? ContainerParent { get; set; }
    public List<Item> ContainerChildren { get; set; } = new();

    public List<ExternalIdentifier> Identifiers { get; set; } = new();
    public List<Contribution> Contributions { get; set; } = new();
    public List<ItemCategory> Categories { get; set; } = new();
    public List<ItemTag> Tags { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();

    // Field-bag access --------------------------------------------------------

    /// <summary>Parse the field bag into a mutable CSL object (without factored relations).</summary>
    public JsonObject FieldBag()
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(FieldsJson) ? "{}" : FieldsJson);
        return node as JsonObject ?? new JsonObject();
    }

    public string? GetField(string name)
    {
        var bag = FieldBag();
        return bag.TryGetPropertyValue(name, out var n) && n is not null ? n.ToString() : null;
    }
}
