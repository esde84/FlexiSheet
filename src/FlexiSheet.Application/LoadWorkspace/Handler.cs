using FlexiSheet.Application.Abstractions;

namespace FlexiSheet.Application.LoadWorkspace;

public sealed class Handler(IWorkspaceRepository repository, TimeProvider clock)
{
    public async Task<Workspace> HandleAsync()
    {
        var workspace = await repository.LoadAsync();
        var updated = workspace.RetainRecentPeriods(DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        if (updated.Periods.Count == workspace.Periods.Count)
            return workspace;

        await repository.SaveAsync(updated);
        return updated;
    }
}
