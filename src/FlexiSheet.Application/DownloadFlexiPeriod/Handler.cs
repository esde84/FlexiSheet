using FlexiSheet.Application.Abstractions;
using FlexiSheet.Application.Sharing;

namespace FlexiSheet.Application.DownloadFlexiPeriod;

public sealed record Request(Guid PeriodId);

public sealed class Handler(IWorkspaceRepository repository, IWorkspaceFileCodec codec,
    IFileDelivery delivery)
{
    public async Task HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var export = PeriodFile.Create(workspace, request.PeriodId, codec);
        await delivery.DownloadAsync(export.File);
    }
}
