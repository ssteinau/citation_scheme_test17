namespace Recite.Core.Csl;

/// <summary>
/// The subset of CSL-JSON item types Recite understands, plus mapping helpers.
/// Types are plain strings in CSL-JSON; these constants keep call-sites honest.
/// </summary>
public static class CslType
{
    public const string ArticleJournal = "article-journal";
    public const string ArticleMagazine = "article-magazine";
    public const string ArticleNewspaper = "article-newspaper";
    public const string PaperConference = "paper-conference";
    public const string Chapter = "chapter";
    public const string Book = "book";
    public const string Thesis = "thesis";
    public const string Report = "report";
    public const string Standard = "standard";
    public const string Webpage = "webpage";
    public const string Dataset = "dataset";
    public const string Patent = "patent";
    public const string Document = "document";

    /// <summary>Container-shaped types: proceedings volumes and edited books that
    /// contributions crossref into (see PLAN §3 "three container shapes").</summary>
    public static readonly IReadOnlySet<string> ContainerTypes = new HashSet<string>
    {
        Book, "proceedings", "collection",
    };

    public static bool IsContainer(string type) => ContainerTypes.Contains(type);

    /// <summary>All types offered in the editor's type picker.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        ArticleJournal, PaperConference, Chapter, Book, "proceedings", "collection",
        Thesis, Report, Standard, ArticleMagazine, ArticleNewspaper, Webpage,
        Dataset, Patent, Document,
    };
}
