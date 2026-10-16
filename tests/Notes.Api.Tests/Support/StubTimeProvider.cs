namespace Notes.Api.Tests.Support;

public sealed class StubTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public StubTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetUtcNow(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan by) => _now += by;
}
