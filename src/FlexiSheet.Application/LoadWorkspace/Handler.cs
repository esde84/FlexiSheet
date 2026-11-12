using FlexiSheet.Application.Abstractions;

namespace FlexiSheet.Application.LoadWorkspace;

public sealed class Handler(IWorkspaceRepository repository, TimeProvider clock)
{
    public async Task<Workspace> HandleAsync()
    {
        var workspace = await repository.LoadAsync();
        var cutoff = DateOnly.FromDateTime(clock.GetLocalNow().DateTime).AddYears(-2);
        var retained = workspace.Periods.Where(period => period.EndDate >= cutoff).ToArray();
        if (retained.Length == workspace.Periods.Count)
            return workspace;

        var updated = workspace with { Periods = retained };
        await repository.SaveAsync(updated);
        return updated;
    }
}
