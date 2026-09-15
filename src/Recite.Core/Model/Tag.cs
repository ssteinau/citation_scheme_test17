namespace Recite.Core.Model;

/// <summary>A flat, optional keyword (PLAN §2 "tags remain available but flat and optional").</summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<ItemTag> Items { get; set; } = new();
}

public class ItemTag
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
