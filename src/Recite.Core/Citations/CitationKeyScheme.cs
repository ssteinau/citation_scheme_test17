using System.Text;
using Recite.Core.Csl;

namespace Recite.Core.Citations;

/// <summary>
/// Generates stable, collision-safe citation keys from a scheme template (PLAN §7).
/// Default scheme <c>[auth][year][shorttitle]</c>. Placeholders:
/// <list type="bullet">
/// <item><c>[auth]</c> first author's family name</item>
/// <item><c>[auth.etal]</c> first author, with an <c>EtAl</c> suffix when there are more</item>
/// <item><c>[authors]</c> up to three families concatenated (<c>[authorsN]</c> for N)</item>
/// <item><c>[authorsall]</c> every family concatenated</item>
/// <item><c>[authIniN]</c> the first N letters of each author's family, concatenated</item>
/// <item><c>[year]</c> issued year (or "nd")</item>
/// <item><c>[shorttitle]</c> first significant title word, capitalised</item>
/// <item><c>[veryshorttitle]</c> same but lowercased</item>
/// <item><c>[title]</c> / <c>[title:N]</c> all / first N significant title words</item>
/// <item><c>[journal]</c> significant words of the container title</item>
/// </list>
/// Any placeholder accepts trailing <c>:</c>-separated modifiers, ported from Zotero's
/// Better BibTeX: <c>:lower</c>, <c>:upper</c>, and <c>:abbr</c> (initials of each word).
/// A bare numeric modifier (<c>[title:3]</c>) selects the first N words.
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

    // Punctuation that separates words inside titles / journal names.
    private static readonly char[] WordSeparators =
        { ' ', '-', ':', ';', ',', '.', '/', '(', ')' };

    /// <summary>
    /// Expand a single <c>[token]</c>. A token is <c>name[:mod][:mod…]</c> where
    /// <c>name</c> may carry a trailing count (<c>authIni2</c>) and each modifier is either
    /// a Better BibTeX filter (<c>lower</c>/<c>upper</c>/<c>abbr</c>) or a bare number that
    /// selects the first N items.
    /// </summary>
    private static string Expand(string token, CslDocument doc)
    {
        var parts = token.Split(':');
        var (name, count) = SplitTrailingCount(parts[0]);

        var modifiers = new List<string>();
        foreach (var mod in parts.Skip(1))
        {
            if (int.TryParse(mod, out var n)) count = n;   // e.g. [title:3]
            else modifiers.Add(mod);
        }

        var value = name switch
        {
            "auth" => AuthFamily(doc, 0),
            "auth.etal" => AuthEtAl(doc),
            "authors" => Authors(doc, count ?? 3),
            "authorsall" => Authors(doc, int.MaxValue),
            "authini" => AuthInitials(doc, count ?? 1),
            "year" => doc.Issued?.Year?.ToString() ?? "nd",
            "shorttitle" => Capitalize(TextNormalizer.FirstSignificantWord(doc.Title)),
            "veryshorttitle" => TextNormalizer.FirstSignificantWord(doc.Title).ToLowerInvariant(),
            "title" => TitleWords(doc, count ?? int.MaxValue),
            "journal" => Journal(doc),
            "type" => doc.Type,
            _ => "",
        };

        foreach (var mod in modifiers)
            value = ApplyModifier(value, mod);
        return value;
    }

    /// <summary>Split a placeholder name from a trailing integer, e.g. <c>authIni2</c> → (authini, 2).</summary>
    private static (string name, int? count) SplitTrailingCount(string spec)
    {
        int i = spec.Length;
        while (i > 0 && char.IsDigit(spec[i - 1])) i--;
        if (i == spec.Length || i == 0) return (spec, null);
        return int.TryParse(spec[i..], out var n) ? (spec[..i], n) : (spec, null);
    }

    /// <summary>Apply a Better BibTeX-style filter to an already-rendered fragment.</summary>
    private static string ApplyModifier(string value, string modifier) => modifier switch
    {
        "lower" => value.ToLowerInvariant(),
        "upper" => value.ToUpperInvariant(),
        "abbr" => Abbreviate(value),
        _ => value,
    };

    /// <summary>Initials of each space-separated word, e.g. "Neural Information" → "NI".</summary>
    private static string Abbreviate(string value)
    {
        var sb = new StringBuilder();
        foreach (var word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            sb.Append(char.ToUpperInvariant(word[0]));
        return sb.ToString();
    }

    /// <summary>Significant (non-stop-word) words of a text, ASCII-folded and capitalised.</summary>
    private static IEnumerable<string> SignificantWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (var raw in text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TextNormalizer.StopWords.Contains(raw)) continue;
            var word = Capitalize(TextNormalizer.AsciiAlnum(raw));
            if (word.Length > 0) yield return word;
        }
    }

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

    /// <summary>First author's family, with an <c>EtAl</c> suffix when the list is longer.</summary>
    private static string AuthEtAl(CslDocument doc)
    {
        var authors = doc.Authors.Count > 0 ? doc.Authors : doc.Editors;
        if (authors.Count == 0) return "Anon";
        var first = AuthFamily(doc, 0);
        return authors.Count > 1 ? first + "EtAl" : first;
    }

    /// <summary>Concatenate the first <paramref name="n"/> letters of every author's family.</summary>
    private static string AuthInitials(CslDocument doc, int n)
    {
        var authors = doc.Authors.Count > 0 ? doc.Authors : doc.Editors;
        if (authors.Count == 0 || n <= 0) return "Anon";
        var sb = new StringBuilder();
        foreach (var a in authors)
        {
            var fam = TextNormalizer.AsciiAlnum(a.IsLiteral ? a.Literal : a.Family);
            if (fam.Length == 0) continue;
            sb.Append(Capitalize(fam[..Math.Min(n, fam.Length)]));
        }
        return sb.Length == 0 ? "Anon" : sb.ToString();
    }

    /// <summary>First <paramref name="max"/> significant title words, capitalised.</summary>
    private static string TitleWords(CslDocument doc, int max) =>
        string.Join(" ", SignificantWords(doc.Title).Take(max));

    /// <summary>Significant words of the container (journal) title.</summary>
    private static string Journal(CslDocument doc) =>
        string.Join(" ", SignificantWords(doc.ContainerTitle ?? doc.CollectionTitle));

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
