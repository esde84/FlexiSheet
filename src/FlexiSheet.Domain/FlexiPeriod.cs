namespace FlexiSheet.Domain;

public sealed class FlexiPeriod
{
    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate => StartDate.AddDays(WorkingPattern.DaysPerPeriod - 1);
    public IReadOnlyList<TimeSpan> ExpectedTime { get; }
    public TimeSpan CarryOver { get; }
    public IReadOnlyList<WorkSession> Sessions { get; }
    public TimeSpan TotalExpectedTime => TimeSpan.FromTicks(ExpectedTime.Sum(time => time.Ticks));
    public TimeSpan TotalWorkedTime => TimeSpan.FromTicks(Sessions.Sum(session => session.Duration.Ticks));
    public TimeSpan Balance => CarryOver + (TotalWorkedTime - TotalExpectedTime);
    public IReadOnlyList<FlexiDay> Days => Array.AsReadOnly(Enumerable.Range(0, WorkingPattern.DaysPerPeriod)
        .Select(index => new FlexiDay(StartDate.AddDays(index), ExpectedTime[index],
            Sessions.Where(session => session.Date == StartDate.AddDays(index)))).ToArray());
    public IReadOnlyList<FlexiWeek> Weeks => Array.AsReadOnly(Days.Chunk(7).Select(days => new FlexiWeek(days)).ToArray());

    public FlexiPeriod(Guid id, DateOnly startDate, IEnumerable<TimeSpan> expectedTime, TimeSpan carryOver,
        IEnumerable<WorkSession>? sessions = null)
    {
        if (id == Guid.Empty)
            throw new RuleViolationException("A flexi period must have an identifier.");
        if (startDate.DayOfWeek != DayOfWeek.Monday ||
            startDate > DateOnly.MaxValue.AddDays(-(WorkingPattern.DaysPerPeriod - 1)))
            throw new RuleViolationException("A flexi period must start on a Monday and contain 28 valid dates.");
        if (carryOver.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new RuleViolationException("Enter carry-over in whole minutes.");

        Id = id;
        StartDate = startDate;
        ExpectedTime = new WorkingPattern(expectedTime).ExpectedTime;
        CarryOver = carryOver;
        var recorded = (sessions ?? []).OrderBy(session => session.Date).ThenBy(session => session.StartTime).ToArray();
        if (recorded.Select(session => session.Id).Distinct().Count() != recorded.Length)
            throw new RuleViolationException("Work sessions must have unique identifiers within a period.");
        if (recorded.Any(session => session.Date < StartDate || session.Date > EndDate))
            throw new RuleViolationException("Record the session on a date within this flexi period.");
        for (var index = 1; index < recorded.Length; index++)
            if (recorded[index - 1].Overlaps(recorded[index]))
                throw new RuleViolationException("Work sessions on the same day must not overlap. Adjacent sessions are allowed.");
        Sessions = Array.AsReadOnly(recorded);
        try
        {
            _ = Balance;
        }
        catch (OverflowException)
        {
            throw new RuleViolationException("The resulting balance exceeds the supported duration range. Adjust the carry-over.");
        }
    }

    public bool Overlaps(FlexiPeriod other) => StartDate <= other.EndDate && EndDate >= other.StartDate;
}
