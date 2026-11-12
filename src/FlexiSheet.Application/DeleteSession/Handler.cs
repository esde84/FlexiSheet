using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.DeleteSession;

public sealed record Request(Guid PeriodId, Guid SessionId);

public sealed class Handler(IWorkspaceRepository repository)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var period = workspace.Periods.SingleOrDefault(item => item.Id == request.PeriodId)
            ?? throw new RuleViolationException("This flexi period no longer exists. Reload the page.");
        if (period.Sessions.All(session => session.Id != request.SessionId))
            throw new RuleViolationException("This work session no longer exists. Reload the page.");

        var replacement = new FlexiPeriod(period.Id, period.StartDate, period.ExpectedTime, period.CarryOver,
            period.Sessions.Where(session => session.Id != request.SessionId));
        var updated = workspace with
        {
            Periods = workspace.Periods.Select(item => item.Id == period.Id ? replacement : item).ToArray()
        };
        await repository.SaveAsync(updated);
        return updated;
    }
}
