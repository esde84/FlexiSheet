using FlexiSheet.Application.Abstractions;
using FlexiSheet.Application.Sharing;

namespace FlexiSheet.Application.DownloadFlexiPeriod;

public sealed record Request(Guid PeriodId);

public sealed class Handler(IWorkspaceRepository repository, IWorkspaceFileCodec codec,
    IFileDelivery delivery, TimeProvider clock)
{
    public async Task HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var export = CompletedPeriodFile.Create(workspace, request.PeriodId,
            DateOnly.FromDateTime(clock.GetLocalNow().DateTime), codec);
        await delivery.DownloadAsync(export.File);
    }
}
