using Recite.Core.Csl;


// Sample data for the citation-key showcase, kept out of Program.cs so the entry
// point stays a thin harness.
//
// The new tokens ([auth.etal], [authIni2], [title:N], [journal], :lower/:abbr)
// render empty today and fill in after the Better BibTeX port
// (CRODOX-SHOWCASE-CANDIDATES.md #1); the existing tokens stay put.
public static class ExampleData
{
    public static readonly string[] Templates =
    {
    "[auth][year]",          // existing
    "[authors][year]",       // existing
    "[auth.etal][year]",     // new: first author + EtAl
    "[authIni2][year]",      // new: 2-letter initials
    "[title:3]",             // new: first 3 title words
    "[journal:abbr][year]",  // new: abbreviated journal
    "[auth:lower][year]",    // new: :lower modifier
};

    public static CslDocument SampleDocument()
    {
        var doc = new CslDocument { Title = "Attention Is All You Need" };
        doc.ContainerTitle = "Advances in Neural Information Processing Systems";
        doc.SetDate(CslDocument.IssuedField, CslDate.FromYear(2017));
        doc.SetNames(CslDocument.AuthorRole, new[]
        {
        new CslName { Family = "Vaswani", Given = "Ashish" },
        new CslName { Family = "Shazeer", Given = "Noam" },
        new CslName { Family = "Parmar",  Given = "Niki" },
    });
        return doc;
    }
}
