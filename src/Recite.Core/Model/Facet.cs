namespace Recite.Core.Model;

/// <summary>
/// A named, independent category tree — an orthogonal facet (Approach, Content Kind,
/// Publication, manuscript structure…). Items are classified under many facets at once
/// (PLAN §3 "faceted categories").
/// </summary>
public class Facet
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public int Order { get; set; }

    public List<Category> Categories { get; set; } = new();
}

/// <summary>A node in a facet's ordered adjacency-list tree.</summary>
public class Category
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public int FacetId { get; set; }
    public Facet? Facet { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();

    public string Name { get; set; } = "";
    public int Order { get; set; }

    public List<ItemCategory> Items { get; set; } = new();
}

/// <summary>Ordered many-to-many membership of an item in a category node.</summary>
public class ItemCategory
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int Order { get; set; }
}
