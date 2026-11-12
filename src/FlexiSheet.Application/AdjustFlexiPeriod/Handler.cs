using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.AdjustFlexiPeriod;

public sealed record Request(Guid PeriodId, IReadOnlyList<TimeSpan> ExpectedTime, TimeSpan CarryOver);

public sealed class Handler(IWorkspaceRepository repository)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var existing = workspace.Periods.SingleOrDefault(period => period.Id == request.PeriodId)
            ?? throw new RuleViolationException("This flexi period no longer exists. Reload the page.");
        var replacement = new FlexiPeriod(existing.Id, existing.StartDate, request.ExpectedTime, request.CarryOver);
        var updated = workspace with
        {
            Periods = workspace.Periods.Select(period => period.Id == existing.Id ? replacement : period).ToArray()
        };
        await repository.SaveAsync(updated);
        return updated;
    }
}
