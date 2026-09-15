namespace Recite.Core.Model;

/// <summary>Ordered link of a <see cref="Person"/> to an <see cref="Item"/> in a role.</summary>
public class Contribution
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public ContributorRole Role { get; set; } = ContributorRole.Author;

    /// <summary>Position within its role (0-based); preserves author order.</summary>
    public int Order { get; set; }
}
