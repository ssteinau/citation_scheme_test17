using Recite.Core.Csl;

namespace Recite.Core.Model;

/// <summary>
/// A normalized contributor. Persons are deduped on <see cref="UniqueKey"/> with a
/// manual-merge fallback (PLAN §3 "person reconciliation").
/// </summary>
public class Person
{
    public int Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public string? Family { get; set; }
    public string? Given { get; set; }
    public string? DroppingParticle { get; set; }
    public string? NonDroppingParticle { get; set; }
    public string? Suffix { get; set; }

    /// <summary>Institutional / unparsed name; when set the structured parts are empty.</summary>
    public string? Literal { get; set; }

    /// <summary>Normalized dedup key (see <see cref="ComputeKey"/>); unique index in the DB.</summary>
    public string UniqueKey { get; set; } = "";

    public List<Contribution> Contributions { get; set; } = new();

    public CslName ToCslName() => new()
    {
        Family = Family,
        Given = Given,
        DroppingParticle = DroppingParticle,
        NonDroppingParticle = NonDroppingParticle,
        Suffix = Suffix,
        Literal = Literal,
    };

    public static Person FromCslName(CslName n)
    {
        var p = new Person
        {
            Family = n.Family,
            Given = n.Given,
            DroppingParticle = n.DroppingParticle,
            NonDroppingParticle = n.NonDroppingParticle,
            Suffix = n.Suffix,
            Literal = n.IsLiteral ? n.Literal : null,
        };
        p.UniqueKey = ComputeKey(p);
        return p;
    }

    public string DisplayName() => ToCslName().DisplayName();

    /// <summary>
    /// Folded key for automatic dedup: lowercased, accent- and punctuation-stripped
    /// "family|given". The full given name is used, so "Smith, John" and "Smith, Jane"
    /// stay distinct; ambiguous initial-vs-full cases ("Hull, R." vs "Hull, Richard")
    /// don't auto-merge and instead surface in the manual person-reconciliation queue.
    /// </summary>
    public static string ComputeKey(Person p)
    {
        if (!string.IsNullOrWhiteSpace(p.Literal))
            return "@" + TextNormalizer.Fold(p.Literal!);

        var fam = TextNormalizer.Fold(
            string.Join(' ', new[] { p.NonDroppingParticle, p.Family }
                .Where(s => !string.IsNullOrWhiteSpace(s))));
        var given = TextNormalizer.Fold(p.Given ?? "");
        return $"{fam}|{given}";
    }

    /// <summary>Folded family key (with particle) for blocking person-dedup candidates.</summary>
    public static string FamilyKey(Person p) =>
        !string.IsNullOrWhiteSpace(p.Literal)
            ? "@" + TextNormalizer.Fold(p.Literal!)
            : TextNormalizer.Fold(string.Join(' ',
                new[] { p.NonDroppingParticle, p.Family }.Where(s => !string.IsNullOrWhiteSpace(s))));
}
