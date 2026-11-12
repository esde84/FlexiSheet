namespace FlexiSheet.Domain;

public sealed class FlexiDay
{
    public DateOnly Date { get; }
    public TimeSpan ExpectedTime { get; }
    public IReadOnlyList<WorkSession> Sessions { get; }
    public TimeSpan WorkedTime => TimeSpan.FromTicks(Sessions.Sum(session => session.Duration.Ticks));
    public TimeSpan Balance => WorkedTime - ExpectedTime;

    internal FlexiDay(DateOnly date, TimeSpan expectedTime, IEnumerable<WorkSession> sessions)
    {
        Date = date;
        ExpectedTime = expectedTime;
        Sessions = Array.AsReadOnly(sessions.OrderBy(session => session.StartTime).ToArray());
    }
}
