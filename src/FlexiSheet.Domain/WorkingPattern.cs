namespace FlexiSheet.Domain;

public sealed class WorkingPattern
{
    public const int DaysPerPeriod = 28;

    public IReadOnlyList<TimeSpan> ExpectedTime { get; }
    public TimeSpan TotalExpectedTime => TimeSpan.FromTicks(ExpectedTime.Sum(time => time.Ticks));

    public WorkingPattern(IEnumerable<TimeSpan> expectedTime)
    {
        var days = expectedTime.ToArray();
        if (days.Length != DaysPerPeriod)
            throw new RuleViolationException("Enter expected working time for all 28 days.");

        foreach (var time in days)
            ValidateDailyTime(time);

        ExpectedTime = Array.AsReadOnly(days);
    }

    public static void ValidateDailyTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero || time > TimeSpan.FromDays(1) ||
            time.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new RuleViolationException("Daily expected time must be between 0:00 and 24:00, in whole minutes.");
    }
}
