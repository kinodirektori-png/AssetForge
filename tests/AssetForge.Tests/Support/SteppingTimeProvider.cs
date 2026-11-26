namespace AssetForge.Tests.Support;

/// <summary>A clock that moves forward one second each time it is read, so every timestamp in a test is distinct and ordered.</summary>
public sealed class SteppingTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private long _reads;

    public override DateTimeOffset GetUtcNow() => Start.AddSeconds(Interlocked.Increment(ref _reads) - 1);
}
