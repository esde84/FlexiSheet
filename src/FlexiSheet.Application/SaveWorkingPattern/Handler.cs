using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.SaveWorkingPattern;

public sealed record Request(IReadOnlyList<TimeSpan> ExpectedTime);

public sealed class Handler(IWorkspaceRepository repository)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        var pattern = new WorkingPattern(request.ExpectedTime);
        var workspace = await repository.LoadAsync();
        var updated = workspace with { WorkingPattern = pattern };
        await repository.SaveAsync(updated);
        return updated;
    }
}
