namespace Recite.Core.Model;

/// <summary>
/// A linked file (usually a PDF) stored under the library's <c>files/</c> folder and
/// referenced by relative path. Opened in the system viewer; no in-app reader (PLAN §5).
/// </summary>
public class Attachment
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    /// <summary>Path relative to the library's <c>files/</c> directory.</summary>
    public string RelativePath { get; set; } = "";

    public string? Title { get; set; }
    public int Order { get; set; }
}
