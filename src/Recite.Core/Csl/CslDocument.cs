using System.Text.Json;
using System.Text.Json.Nodes;

namespace Recite.Core.Csl;

/// <summary>
/// A single CSL-JSON reference. This is Recite's canonical interchange representation:
/// every format adapter converts to/from it, the projection engine transforms it, and
/// the domain model reconstructs itself from it. Backed by a live <see cref="JsonObject"/>
/// so unknown CSL fields round-trip losslessly.
/// </summary>
public sealed class CslDocument
{
    public JsonObject Root { get; }

    public CslDocument() => Root = new JsonObject();
    public CslDocument(JsonObject root) => Root = root;

    public CslDocument Clone() =>
        new((JsonObject)JsonNode.Parse(Root.ToJsonString())!);

    // ---- scalar fields ------------------------------------------------------

    public string? Id
    {
        get => GetString("id");
        set => SetString("id", value);
    }

    public string Type
    {
        get => GetString("type") ?? CslType.Document;
        set => SetString("type", value);
    }

    public string? GetString(string field) =>
        Root.TryGetPropertyValue(field, out var n) && n is not null ? n.GetValue<string>() : null;

    public void SetString(string field, string? value)
    {
        if (string.IsNullOrEmpty(value)) Root.Remove(field);
        else Root[field] = value;
    }

    // Common CSL string fields, surfaced for ergonomics.
    public string? Title { get => GetString("title"); set => SetString("title", value); }
    public string? ContainerTitle { get => GetString("container-title"); set => SetString("container-title", value); }
    public string? CollectionTitle { get => GetString("collection-title"); set => SetString("collection-title", value); }
    public string? Volume { get => GetString("volume"); set => SetString("volume", value); }
    public string? Issue { get => GetString("issue"); set => SetString("issue", value); }
    public string? Page { get => GetString("page"); set => SetString("page", value); }
    public string? Publisher { get => GetString("publisher"); set => SetString("publisher", value); }
    public string? PublisherPlace { get => GetString("publisher-place"); set => SetString("publisher-place", value); }
    public string? Edition { get => GetString("edition"); set => SetString("edition", value); }
    public string? Abstract { get => GetString("abstract"); set => SetString("abstract", value); }
    public string? Doi { get => GetString("DOI"); set => SetString("DOI", value); }
    public string? Isbn { get => GetString("ISBN"); set => SetString("ISBN", value); }
    public string? Issn { get => GetString("ISSN"); set => SetString("ISSN", value); }
    public string? Url { get => GetString("URL"); set => SetString("URL", value); }
    public string? Number { get => GetString("number"); set => SetString("number", value); }
    public string? Genre { get => GetString("genre"); set => SetString("genre", value); }
    public string? Note { get => GetString("note"); set => SetString("note", value); }

    // ---- names --------------------------------------------------------------

    public const string AuthorRole = "author";
    public const string EditorRole = "editor";
    public const string TranslatorRole = "translator";

    /// <summary>All CSL name variables Recite recognises, in bibliography-ish order.</summary>
    public static readonly IReadOnlyList<string> NameRoles = new[]
    {
        "author", "editor", "translator", "container-author", "collection-editor",
        "editorial-director", "director", "composer", "recipient", "interviewer",
    };

    public IReadOnlyList<CslName> Names(string role)
    {
        if (Root.TryGetPropertyValue(role, out var node) && node is JsonArray arr)
            return arr.OfType<JsonObject>().Select(NameFromJson).ToList();
        return Array.Empty<CslName>();
    }

    public IReadOnlyList<CslName> Authors => Names(AuthorRole);
    public IReadOnlyList<CslName> Editors => Names(EditorRole);

    public void SetNames(string role, IEnumerable<CslName> names)
    {
        var list = names.Where(n => !n.IsEmpty).ToList();
        if (list.Count == 0) { Root.Remove(role); return; }
        var arr = new JsonArray();
        foreach (var n in list) arr.Add(NameToJson(n));
        Root[role] = arr;
    }

    public IEnumerable<string> PresentNameRoles() =>
        NameRoles.Where(r => Root.TryGetPropertyValue(r, out var n) && n is JsonArray a && a.Count > 0);

    // ---- dates --------------------------------------------------------------

    public const string IssuedField = "issued";

    public CslDate? Date(string field)
    {
        if (Root.TryGetPropertyValue(field, out var node) && node is JsonObject obj)
            return DateFromJson(obj);
        return null;
    }

    public CslDate? Issued => Date(IssuedField);

    public void SetDate(string field, CslDate? date)
    {
        if (date is null || date.IsEmpty) { Root.Remove(field); return; }
        Root[field] = DateToJson(date);
    }

    // ---- JSON <-> value-type conversions -----------------------------------

    internal static CslName NameFromJson(JsonObject o) => new()
    {
        Family = Str(o, "family"),
        Given = Str(o, "given"),
        DroppingParticle = Str(o, "dropping-particle"),
        NonDroppingParticle = Str(o, "non-dropping-particle"),
        Suffix = Str(o, "suffix"),
        Literal = Str(o, "literal"),
    };

    internal static JsonObject NameToJson(CslName n)
    {
        var o = new JsonObject();
        if (n.IsLiteral) { o["literal"] = n.Literal; return o; }
        if (!string.IsNullOrWhiteSpace(n.Family)) o["family"] = n.Family;
        if (!string.IsNullOrWhiteSpace(n.Given)) o["given"] = n.Given;
        if (!string.IsNullOrWhiteSpace(n.DroppingParticle)) o["dropping-particle"] = n.DroppingParticle;
        if (!string.IsNullOrWhiteSpace(n.NonDroppingParticle)) o["non-dropping-particle"] = n.NonDroppingParticle;
        if (!string.IsNullOrWhiteSpace(n.Suffix)) o["suffix"] = n.Suffix;
        return o;
    }

    internal static CslDate DateFromJson(JsonObject o)
    {
        if (o.TryGetPropertyValue("date-parts", out var dp) && dp is JsonArray outer &&
            outer.Count > 0 && outer[0] is JsonArray first)
        {
            int? P(int i) => first.Count > i && first[i] is not null && int.TryParse(first[i]!.ToString(), out var v) ? v : null;
            return new CslDate { Year = P(0), Month = P(1), Day = P(2) };
        }
        return new CslDate { Literal = Str(o, "literal") };
    }

    internal static JsonObject DateToJson(CslDate d)
    {
        var o = new JsonObject();
        if (d.Year is not null)
        {
            var parts = new JsonArray { d.Year };
            if (d.Month is not null) parts.Add(d.Month);
            if (d.Day is not null) parts.Add(d.Day);
            o["date-parts"] = new JsonArray { parts };
        }
        if (!string.IsNullOrWhiteSpace(d.Literal)) o["literal"] = d.Literal;
        return o;
    }

    private static string? Str(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var n) && n is not null ? n.GetValue<string>() : null;

    public string ToJsonString(JsonSerializerOptions? options = null) =>
        Root.ToJsonString(options ?? CslJson.Pretty);
}
