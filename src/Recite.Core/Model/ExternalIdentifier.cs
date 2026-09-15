namespace Recite.Core.Model;

/// <summary>
/// An external matching signal (DOI, ISBN, ISSN, arXiv, PubMed, URL). Never the
/// primary key — identity is <see cref="Item.Uuid"/> (PLAN §3). Uniqueness is on
/// (scheme, value) so an identifier reconciles duplicates.
/// </summary>
public class ExternalIdentifier
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public string Scheme { get; set; } = "";
    public string Value { get; set; } = "";

    public static class Schemes
    {
        public const string Doi = "doi";
        public const string Isbn = "isbn";
        public const string Issn = "issn";
        public const string ArXiv = "arxiv";
        public const string PubMed = "pubmed";
        public const string Url = "url";
    }

    /// <summary>Fold identifier values so "10.1/AB" and "10.1/ab" reconcile.</summary>
    public static string Normalize(string scheme, string value)
    {
        value = value.Trim();
        return scheme switch
        {
            Schemes.Doi => value.ToLowerInvariant()
                .Replace("https://doi.org/", "").Replace("http://doi.org/", "")
                .Replace("doi:", "").Trim(),
            Schemes.Isbn => new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant(),
            Schemes.Issn => new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpperInvariant(),
            Schemes.ArXiv => value.ToLowerInvariant().Replace("arxiv:", "").Trim(),
            Schemes.PubMed => new string(value.Where(char.IsDigit).ToArray()),
            _ => value,
        };
    }
}
