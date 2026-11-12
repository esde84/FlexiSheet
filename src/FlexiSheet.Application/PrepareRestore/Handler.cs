using FlexiSheet.Application.Abstractions;

namespace FlexiSheet.Application.PrepareRestore;

public sealed record Request(string Json);
public sealed record Preview(string Json, int RetainedPeriods, int ExpiredPeriods, bool HasWorkingPattern);

public sealed class Handler(IWorkspaceFileCodec codec, TimeProvider clock)
{
    public Preview Handle(Request request)
    {
        var imported = codec.Deserialize(request.Json);
        var retained = imported.RetainRecentPeriods(DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
        return new Preview(request.Json, retained.Periods.Count,
            imported.Periods.Count - retained.Periods.Count, imported.WorkingPattern is not null);
    }
}
