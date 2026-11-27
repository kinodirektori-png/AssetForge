namespace AssetForge.Application;

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

    /// <summary>
    /// Adds an error when <paramref name="value"/> is over its limit and returns whether it did. A value that is within the
    /// character limit but over the size cap gets its own message, so the text never names a limit the value is under.
    /// </summary>
    public bool TryAddLengthError(string key, string subject, string value, int maxCharacters)
    {
        switch (TextLength.Check(value, maxCharacters))
        {
            case TextLimit.TooManyCharacters:
                Add(key, $"{subject} must be at most {maxCharacters} characters.");
                return true;
            case TextLimit.TooLarge:
                Add(key, $"{subject} is too large.");
                return true;
            default:
                return false;
        }
    }

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
