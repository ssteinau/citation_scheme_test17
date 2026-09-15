using System.Text;

namespace Recite.Core.Csl;

/// <summary>
/// A CSL-JSON name object. Either the structured parts are set, or <see cref="Literal"/>
/// carries an institutional / unparseable name ("IEEE", "World Health Organization").
/// </summary>
public sealed record CslName
{
    public string? Family { get; init; }
    public string? Given { get; init; }
    public string? DroppingParticle { get; init; }
    public string? NonDroppingParticle { get; init; }
    public string? Suffix { get; init; }
    public string? Literal { get; init; }

    public bool IsLiteral => !string.IsNullOrWhiteSpace(Literal);

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Family) && string.IsNullOrWhiteSpace(Given) &&
        string.IsNullOrWhiteSpace(Literal);

    /// <summary>"Family, Given" style, used for display and as a reconciliation seed.</summary>
    public string DisplayName()
    {
        if (IsLiteral) return Literal!.Trim();
        var non = string.IsNullOrWhiteSpace(NonDroppingParticle) ? "" : NonDroppingParticle!.Trim() + " ";
        var fam = (non + (Family ?? "")).Trim();
        var giv = string.Join(' ',
            new[] { Given, DroppingParticle, Suffix }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (fam.Length == 0) return giv;
        return giv.Length == 0 ? fam : $"{fam}, {giv}";
    }

    /// <summary>"Given Family" order, used in bibliography rendering when required.</summary>
    public string GivenFamily()
    {
        if (IsLiteral) return Literal!.Trim();
        var sb = new StringBuilder();
        void Add(string? s) { if (!string.IsNullOrWhiteSpace(s)) { if (sb.Length > 0) sb.Append(' '); sb.Append(s!.Trim()); } }
        Add(Given);
        Add(DroppingParticle);
        Add(NonDroppingParticle);
        Add(Family);
        Add(Suffix);
        return sb.ToString();
    }

    /// <summary>
    /// Parse "Family, Given" or "Given Family" into parts. BibTeX-style "von" particles
    /// and "Jr" suffixes are handled at a best-effort level.
    /// </summary>
    public static CslName Parse(string raw)
    {
        raw = raw.Trim();
        if (raw.Length == 0) return new CslName { Literal = "" };

        // Institutional names are wrapped in braces or contain no comma and look like an org.
        if (raw.StartsWith('{') && raw.EndsWith('}'))
            return new CslName { Literal = raw[1..^1].Trim() };

        var commaParts = SplitTopLevel(raw, ',');
        if (commaParts.Count == 1)
        {
            // "Given ... Family" — last whitespace token is the family name.
            var tokens = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1)
                return new CslName { Family = tokens[0] };
            var (nonDrop, familyStart) = ExtractParticles(tokens);
            var family = string.Join(' ', tokens.Skip(familyStart));
            var givenNames = string.Join(' ', tokens.Take(nonDrop));
            var ndp = string.Join(' ', tokens.Skip(nonDrop).Take(familyStart - nonDrop));
            return new CslName
            {
                Given = string.IsNullOrWhiteSpace(givenNames) ? null : givenNames,
                NonDroppingParticle = string.IsNullOrWhiteSpace(ndp) ? null : ndp,
                Family = family,
            };
        }

        // "Family, Given" or "Family, Suffix, Given"
        var familyPart = commaParts[0].Trim();
        string? suffix = null;
        string? given;
        if (commaParts.Count >= 3)
        {
            suffix = commaParts[1].Trim();
            given = commaParts[2].Trim();
        }
        else
        {
            given = commaParts[1].Trim();
        }

        // von particle inside the family part ("van der Berg" written as "van der Berg, Jan")
        string? ndParticle = null;
        var fTokens = familyPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fTokens.Length > 1)
        {
            var lead = fTokens.TakeWhile(IsParticle).Count();
            if (lead > 0 && lead < fTokens.Length)
            {
                ndParticle = string.Join(' ', fTokens.Take(lead));
                familyPart = string.Join(' ', fTokens.Skip(lead));
            }
        }

        return new CslName
        {
            Family = familyPart,
            Given = string.IsNullOrWhiteSpace(given) ? null : given,
            Suffix = suffix,
            NonDroppingParticle = ndParticle,
        };
    }

    private static (int givenCount, int familyStart) ExtractParticles(string[] tokens)
    {
        // Scan from the first token: given names until we hit a lowercase particle or the last token.
        int i = 0;
        while (i < tokens.Length - 1 && !IsParticle(tokens[i])) i++;
        int givenCount = i;
        int familyStart = i;
        while (familyStart < tokens.Length - 1 && IsParticle(tokens[familyStart])) familyStart++;
        return (givenCount, familyStart);
    }

    private static bool IsParticle(string token) =>
        token.Length > 0 && char.IsLower(token[0]) &&
        token is "von" or "van" or "der" or "de" or "den" or "del" or "della" or "di"
            or "da" or "dos" or "la" or "le" or "du" or "ter" or "ten" or "af" or "zu";

    private static List<string> SplitTopLevel(string s, char sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        int depth = 0;
        foreach (var c in s)
        {
            if (c == '{') depth++;
            else if (c == '}') depth = Math.Max(0, depth - 1);
            if (c == sep && depth == 0)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }
}
