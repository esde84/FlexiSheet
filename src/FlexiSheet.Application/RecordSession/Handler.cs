using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.RecordSession;

public sealed record Request(Guid PeriodId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
    bool EndsAtMidnight = false, Guid? SessionId = null);

public sealed class Handler(IWorkspaceRepository repository)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var period = workspace.Periods.SingleOrDefault(item => item.Id == request.PeriodId)
            ?? throw new RuleViolationException("This flexi period no longer exists. Reload the page.");
        if (request.SessionId is not null && period.Sessions.All(session => session.Id != request.SessionId))
            throw new RuleViolationException("This work session no longer exists. Reload the page.");

        var recorded = new WorkSession(request.SessionId ?? Guid.NewGuid(), request.Date,
            request.StartTime, request.EndTime, request.EndsAtMidnight);
        var sessions = period.Sessions.Where(session => session.Id != recorded.Id).Append(recorded);
        var replacement = new FlexiPeriod(period.Id, period.StartDate, period.ExpectedTime, period.CarryOver, sessions);
        var updated = workspace with
        {
            Periods = workspace.Periods.Select(item => item.Id == period.Id ? replacement : item).ToArray()
        };
        await repository.SaveAsync(updated);
        return updated;
    }
}
