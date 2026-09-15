using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Recite.Core.Csl;

/// <summary>Shared System.Text.Json options and CSL-JSON (de)serialisation.</summary>
public static class CslJson
{
    /// <summary>Deterministic, human-diffable output for the git-friendly text tree.</summary>
    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Parse a CSL-JSON array (or single object) into documents.</summary>
    public static List<CslDocument> ParseArray(string json)
    {
        var node = JsonNode.Parse(json);
        var result = new List<CslDocument>();
        switch (node)
        {
            case JsonArray arr:
                foreach (var el in arr)
                    if (el is JsonObject o) result.Add(new CslDocument(o.DeepClone().AsObject()));
                break;
            case JsonObject single:
                result.Add(new CslDocument(single.DeepClone().AsObject()));
                break;
        }
        return result;
    }

    public static string WriteArray(IEnumerable<CslDocument> docs)
    {
        var arr = new JsonArray();
        foreach (var d in docs) arr.Add(d.Root.DeepClone());
        return arr.ToJsonString(Pretty);
    }
}
