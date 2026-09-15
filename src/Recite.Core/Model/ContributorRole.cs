namespace Recite.Core.Model;

/// <summary>Contributor roles, aligned to CSL name variables.</summary>
public enum ContributorRole
{
    Author,
    Editor,
    Translator,
    ContainerAuthor,
    CollectionEditor,
    Director,
    Composer,
    Recipient,
}

public static class ContributorRoleExtensions
{
    public static string ToCsl(this ContributorRole role) => role switch
    {
        ContributorRole.Author => "author",
        ContributorRole.Editor => "editor",
        ContributorRole.Translator => "translator",
        ContributorRole.ContainerAuthor => "container-author",
        ContributorRole.CollectionEditor => "collection-editor",
        ContributorRole.Director => "director",
        ContributorRole.Composer => "composer",
        ContributorRole.Recipient => "recipient",
        _ => "author",
    };

    public static ContributorRole FromCsl(string csl) => csl switch
    {
        "author" => ContributorRole.Author,
        "editor" => ContributorRole.Editor,
        "translator" => ContributorRole.Translator,
        "container-author" => ContributorRole.ContainerAuthor,
        "collection-editor" => ContributorRole.CollectionEditor,
        "director" => ContributorRole.Director,
        "composer" => ContributorRole.Composer,
        "recipient" => ContributorRole.Recipient,
        _ => ContributorRole.Author,
    };
}
