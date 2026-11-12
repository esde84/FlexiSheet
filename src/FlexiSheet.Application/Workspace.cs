using FlexiSheet.Domain;

namespace FlexiSheet.Application;

public sealed record Workspace(WorkingPattern? WorkingPattern, IReadOnlyList<FlexiPeriod> Periods)
{
    public static Workspace Empty => new(null, []);

    public Workspace RetainRecentPeriods(DateOnly today) => this with
    {
        Periods = Periods.Where(period => period.EndDate >= today.AddYears(-2)).ToArray()
    };

    public void Validate()
    {
        var ordered = Periods.OrderBy(period => period.StartDate).ToArray();
        if (ordered.Select(period => period.Id).Distinct().Count() != ordered.Length)
            throw new RuleViolationException("Flexi periods must have unique identifiers.");

        for (var index = 1; index < ordered.Length; index++)
            if (ordered[index - 1].Overlaps(ordered[index]))
                throw new RuleViolationException("Flexi periods must not overlap.");
    }
}
