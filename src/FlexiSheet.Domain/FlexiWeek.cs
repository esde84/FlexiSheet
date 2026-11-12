namespace FlexiSheet.Domain;

public sealed class FlexiWeek
{
    public IReadOnlyList<FlexiDay> Days { get; }
    public DateOnly StartDate => Days[0].Date;
    public TimeSpan ExpectedTime => TimeSpan.FromTicks(Days.Sum(day => day.ExpectedTime.Ticks));
    public TimeSpan WorkedTime => TimeSpan.FromTicks(Days.Sum(day => day.WorkedTime.Ticks));
    public TimeSpan Balance => WorkedTime - ExpectedTime;

    internal FlexiWeek(IEnumerable<FlexiDay> days) => Days = Array.AsReadOnly(days.ToArray());
}
