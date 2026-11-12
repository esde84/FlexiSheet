using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Application.RestoreBackup;

public sealed record Request(string Json, bool Confirmed);

public sealed class Handler(IWorkspaceRepository repository, IWorkspaceFileCodec codec, TimeProvider clock)
{
    public async Task<Workspace> HandleAsync(Request request)
    {
        if (!request.Confirmed)
            throw new RuleViolationException("Confirm replacement before restoring a backup.");

        var imported = codec.Deserialize(request.Json)
            .RetainRecentPeriods(DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        // Save against the revision read before confirmation, including an unreadable document being recovered.
        await repository.SaveAsync(imported);
        return imported;
    }
}
