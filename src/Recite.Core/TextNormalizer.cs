using System.Globalization;
using System.Text;

namespace Recite.Core;

/// <summary>
/// Deterministic text folding used by dedup keys, citation keys, and fuzzy matching.
/// Pure and culture-invariant so results are repeatable across machines.
/// </summary>
public static class TextNormalizer
{
    /// <summary>Lowercase, strip diacritics, drop non-alphanumerics, collapse whitespace.</summary>
    public static string Fold(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var stripped = StripDiacritics(input);
        var sb = new StringBuilder(stripped.Length);
        bool lastSpace = false;
        foreach (var ch in stripped)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastSpace = false;
            }
            else if (!lastSpace && sb.Length > 0)
            {
                sb.Append(' ');
                lastSpace = true;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>ASCII-only alphanumerics, no spaces — for citation-key fragments.</summary>
    public static string AsciiAlnum(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var stripped = StripDiacritics(input);
        var sb = new StringBuilder(stripped.Length);
        foreach (var ch in stripped)
            if (ch < 128 && char.IsLetterOrDigit(ch))
                sb.Append(ch);
        return sb.ToString();
    }

    public static string StripDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        // Common ligatures / special letters that don't decompose.
        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("ß", "ss").Replace("æ", "ae").Replace("Æ", "Ae")
            .Replace("ø", "o").Replace("Ø", "O").Replace("ł", "l").Replace("Ł", "L")
            .Replace("đ", "d").Replace("Đ", "D");
    }

    /// <summary>English stop-words dropped when picking a "short title" key fragment.</summary>
    public static readonly IReadOnlySet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "on", "in", "of", "for", "and", "or", "to", "with",
        "at", "by", "from", "as", "into", "via", "using", "toward", "towards",
        "is", "are", "be", "that", "this", "these", "those",
    };

    /// <summary>First significant (non-stop-word) title token, ASCII-folded.</summary>
    public static string FirstSignificantWord(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        foreach (var raw in title.Split(new[] { ' ', '-', ':', ';', ',', '.', '/', '(', ')' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (StopWords.Contains(raw)) continue;
            var word = AsciiAlnum(raw);
            if (word.Length > 0) return word;
        }
        // Fall back to any token.
        foreach (var raw in title.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = AsciiAlnum(raw);
            if (word.Length > 0) return word;
        }
        return "";
    }

    /// <summary>Normalized Levenshtein similarity in [0,1].</summary>
    public static double Similarity(string a, string b)
    {
        a = Fold(a); b = Fold(b);
        if (a.Length == 0 && b.Length == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;
        if (a == b) return 1.0;
        int dist = Levenshtein(a, b);
        int max = Math.Max(a.Length, b.Length);
        return 1.0 - (double)dist / max;
    }

    public static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(prev[j] + 1, curr[j - 1] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }
}
