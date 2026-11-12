using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.Sharing;

internal sealed record CompletedPeriodFile(FlexiPeriod Period, DownloadFile File)
{
    public static CompletedPeriodFile Create(Workspace workspace, Guid periodId, DateOnly today, IWorkspaceFileCodec codec)
    {
        var period = workspace.Periods.SingleOrDefault(item => item.Id == periodId)
            ?? throw new RuleViolationException("This flexi period no longer exists. Reload the page.");
        if (period.EndDate >= today)
            throw new RuleViolationException("A flexi period can only be shared after its end date has passed.");

        var file = new DownloadFile(
            FormattableString.Invariant($"flexisheet-period-{period.StartDate:yyyy-MM-dd}.json"),
            codec.Serialize(new Workspace(null, [period])));
        return new CompletedPeriodFile(period, file);
    }
}
