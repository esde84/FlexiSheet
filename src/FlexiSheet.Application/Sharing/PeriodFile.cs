using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.Sharing;

internal sealed record PeriodFile(FlexiPeriod Period, DownloadFile File)
{
    public static PeriodFile Create(Workspace workspace, Guid periodId, IWorkspaceFileCodec codec)
    {
        var period = workspace.Periods.SingleOrDefault(item => item.Id == periodId)
            ?? throw new RuleViolationException("This flexi period no longer exists. Reload the page.");

        var file = new DownloadFile(
            FormattableString.Invariant($"flexisheet-period-{period.StartDate:yyyy-MM-dd}.json"),
            codec.Serialize(new Workspace(null, [period])));
        return new PeriodFile(period, file);
    }
}
