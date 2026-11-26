namespace AssetForge.Application;

/// <summary>Base for errors whose message is safe to show to the user.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string what) : AppException($"{what} was not found.");

public sealed class ConflictException(string message) : AppException(message);

public sealed class ValidationFailedException(IDictionary<string, string[]> errors)
    : AppException("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public ValidationFailedException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }
}
