namespace Recite.Core.Csl;

/// <summary>
/// A CSL-JSON date. Modelled as up to two endpoints of date-parts
/// ([year, month, day]); single dates use only the first.
/// </summary>
public sealed record CslDate
{
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }

    /// <summary>Free-form literal ("in press", "n.d.") when parts are unknown.</summary>
    public string? Literal { get; init; }

    public bool IsEmpty => Year is null && string.IsNullOrWhiteSpace(Literal);

    public static CslDate FromYear(int year) => new() { Year = year };

    /// <summary>Parse a year out of a loose string like "2020", "2020-05", "c2019".</summary>
    public static CslDate? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        var digits = new string(raw.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        if (digits.Length >= 4 && int.TryParse(digits[..4], out var y))
        {
            int? m = null, d = null;
            var rest = raw[(raw.IndexOf(digits[..4], StringComparison.Ordinal) + 4)..];
            var more = rest.Split(new[] { '-', '/', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (more.Length >= 1 && int.TryParse(more[0], out var mm) && mm is >= 1 and <= 12) m = mm;
            if (more.Length >= 2 && int.TryParse(more[1], out var dd) && dd is >= 1 and <= 31) d = dd;
            return new CslDate { Year = y, Month = m, Day = d };
        }
        return new CslDate { Literal = raw };
    }

    public string DisplayYear() => Year?.ToString() ?? Literal ?? "";
}
