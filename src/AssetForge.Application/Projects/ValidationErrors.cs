namespace AssetForge.Application.Projects;

/// <summary>Collects field errors, then throws one <see cref="ValidationFailedException"/> for all of them.</summary>
internal sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
            _errors[field] = list = [];
        list.Add(message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
            throw new ValidationFailedException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    /// <summary>The JSON name of a property: <c>DetailLevel</c> becomes <c>detailLevel</c>. Use with <c>nameof</c>.</summary>
    public static string Key(string property) => char.ToLowerInvariant(property[0]) + property[1..];

    /// <summary>A property name as words for a message: <c>DetailLevel</c> becomes <c>Detail level</c>.</summary>
    public static string Label(string property)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in property)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ').Append(char.ToLowerInvariant(c));
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
