namespace FlexiSheet.Domain;

public sealed class FlexiPeriod
{
    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate => StartDate.AddDays(WorkingPattern.DaysPerPeriod - 1);
    public IReadOnlyList<TimeSpan> ExpectedTime { get; }
    public TimeSpan CarryOver { get; }
    public TimeSpan TotalExpectedTime => TimeSpan.FromTicks(ExpectedTime.Sum(time => time.Ticks));

    public FlexiPeriod(Guid id, DateOnly startDate, IEnumerable<TimeSpan> expectedTime, TimeSpan carryOver)
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
    }

    public bool Overlaps(FlexiPeriod other) => StartDate <= other.EndDate && EndDate >= other.StartDate;
}
