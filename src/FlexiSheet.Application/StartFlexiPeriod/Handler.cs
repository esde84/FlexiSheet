using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.StartFlexiPeriod;

public sealed record Request(DateOnly StartDate, TimeSpan? CarryOver = null);

public sealed class Handler(IWorkspaceRepository repository, TimeProvider clock)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        if (workspace.WorkingPattern is null)
            throw new RuleViolationException("Save your normal four-week working pattern before starting a period.");

        var previous = workspace.Periods.SingleOrDefault(period =>
            period.EndDate.DayNumber == request.StartDate.DayNumber - 1);
        var carryOver = request.CarryOver ?? previous?.Balance
            ?? throw new RuleViolationException("Enter carry-over manually because the immediately preceding period is not available.");
        var period = new FlexiPeriod(Guid.NewGuid(), request.StartDate,
            workspace.WorkingPattern.ExpectedTime, carryOver);
        var cutoff = DateOnly.FromDateTime(clock.GetLocalNow().DateTime).AddYears(-2);
        if (period.EndDate < cutoff)
            throw new RuleViolationException("This period is outside the two-year retention window. Choose a more recent starting Monday.");
        var updated = workspace with { Periods = [.. workspace.Periods, period] };
        updated.Validate();
        await repository.SaveAsync(updated);
        return updated;
    }
}
