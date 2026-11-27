namespace AssetForge.Application.Projects;

public enum AddResult
{
    Added,

    /// <summary>Another project already has the name (compared ignoring case).</summary>
    NameTaken,

    /// <summary>Another project already has the slug. Happens only when two projects are created at the same moment.</summary>
    SlugTaken,
}
