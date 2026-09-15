using System.Text;
using Recite.Core.Csl;

namespace Recite.Core.Citations;

/// <summary>
/// Generates stable, collision-safe citation keys from a scheme template (PLAN §7).
/// Default scheme <c>[auth][year][shorttitle]</c>. Placeholders:
/// <list type="bullet">
/// <item><c>[auth]</c> first author's family name</item>
/// <item><c>[authors]</c> up to three families concatenated</item>
/// <item><c>[year]</c> issued year (or "nd")</item>
/// <item><c>[shorttitle]</c> first significant title word, capitalised</item>
/// <item><c>[veryshorttitle]</c> same but lowercased</item>
/// </list>
/// Keys are pure functions of the document; collisions get an a/b/c suffix.
/// </summary>
public sealed class CitationKeyScheme
{
    public const string Default = "[auth][year][shorttitle]";

    public string Template { get; }

    public CitationKeyScheme(string? template = null)
        => Template = string.IsNullOrWhiteSpace(template) ? Default : template!;

    /// <summary>Render the base (pre-collision) key for a document.</summary>
    public string BaseKey(CslDocument doc)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i < Template.Length)
        {
            if (Template[i] == '[')
            {
                int end = Template.IndexOf(']', i);
                if (end < 0) { sb.Append(Template[i..]); break; }
                var token = Template[(i + 1)..end].ToLowerInvariant();
                sb.Append(Expand(token, doc));
                i = end + 1;
            }
            else
            {
                sb.Append(Template[i]);
                i++;
            }
        }
        var key = sb.ToString();
        // Keep keys LaTeX-safe.
        key = new string(key.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ':' or '.').ToArray());
        return key.Length == 0 ? "ref" : key;
    }

    private static string Expand(string token, CslDocument doc) => token switch
    {
        "auth" => AuthFamily(doc, 0),
        "authors" => Authors(doc, 3),
        "authorsall" => Authors(doc, int.MaxValue),
        "year" => doc.Issued?.Year?.ToString() ?? "nd",
        "shorttitle" => Capitalize(TextNormalizer.FirstSignificantWord(doc.Title)),
        "veryshorttitle" => TextNormalizer.FirstSignificantWord(doc.Title).ToLowerInvariant(),
        "type" => doc.Type,
        _ => "",
    };

    private static string AuthFamily(CslDocument doc, int index)
    {
        var authors = doc.Authors.Count > 0 ? doc.Authors : doc.Editors;
        if (authors.Count <= index) return "Anon";
        var n = authors[index];
        var fam = n.IsLiteral ? n.Literal : (n.NonDroppingParticle + " " + n.Family);
        var folded = TextNormalizer.AsciiAlnum(fam);
        return folded.Length == 0 ? "Anon" : Capitalize(folded);
    }

    private static string Authors(CslDocument doc, int max)
    {
        var authors = doc.Authors.Count > 0 ? doc.Authors : doc.Editors;
        if (authors.Count == 0) return "Anon";
        var take = authors.Take(max)
            .Select(n => Capitalize(TextNormalizer.AsciiAlnum(n.IsLiteral ? n.Literal : n.Family)))
            .Where(s => s.Length > 0);
        var joined = string.Concat(take);
        if (max != int.MaxValue && authors.Count > max) joined += "EtAl";
        return joined.Length == 0 ? "Anon" : joined;
    }

    private static string Capitalize(string? s) =>
        string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>
    /// Resolve a base key against a set of taken keys, appending a/b/…/z/aa… on collision.
    /// <paramref name="isTaken"/> excludes the item's own current key when re-keying.
    /// </summary>
    public static string Disambiguate(string baseKey, Func<string, bool> isTaken)
    {
        if (!isTaken(baseKey)) return baseKey;
        foreach (var suffix in Suffixes())
        {
            var candidate = baseKey + suffix;
            if (!isTaken(candidate)) return candidate;
        }
        return baseKey; // unreachable in practice
    }

    private static IEnumerable<string> Suffixes()
    {
        for (char c = 'a'; c <= 'z'; c++) yield return c.ToString();
        for (char c1 = 'a'; c1 <= 'z'; c1++)
            for (char c2 = 'a'; c2 <= 'z'; c2++)
                yield return $"{c1}{c2}";
    }
}
