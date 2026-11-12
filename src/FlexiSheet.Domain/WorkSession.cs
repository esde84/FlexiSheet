namespace FlexiSheet.Domain;

public sealed class WorkSession
{
    public Guid Id { get; }
    public DateOnly Date { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }
    public bool EndsAtMidnight { get; }
    public TimeSpan EndOffset => EndsAtMidnight ? TimeSpan.FromDays(1) : EndTime.ToTimeSpan();
    public TimeSpan Duration => EndOffset - StartTime.ToTimeSpan();

    public WorkSession(Guid id, DateOnly date, TimeOnly startTime, TimeOnly endTime, bool endsAtMidnight = false)
    {
        if (id == Guid.Empty)
            throw new RuleViolationException("A work session must have an identifier.");
        if (startTime.Ticks % TimeSpan.TicksPerMinute != 0 || endTime.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new RuleViolationException("Enter session times in whole minutes.");
        if (endsAtMidnight && endTime != TimeOnly.MinValue)
            throw new RuleViolationException("An end-of-day session must finish at 24:00.");
        if (!endsAtMidnight && endTime <= startTime)
            throw new RuleViolationException("The end time must be after the start time. Use 24:00 to finish at midnight and a new session for the next day.");

        Id = id;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        EndsAtMidnight = endsAtMidnight;
    }

    public bool Overlaps(WorkSession other) => Date == other.Date &&
        StartTime.ToTimeSpan() < other.EndOffset && other.StartTime.ToTimeSpan() < EndOffset;
}
