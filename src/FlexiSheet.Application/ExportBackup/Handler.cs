using FlexiSheet.Application.Abstractions;

namespace FlexiSheet.Application.ExportBackup;

public sealed class Handler(IWorkspaceRepository repository, IWorkspaceFileCodec codec,
    IFileDelivery delivery, TimeProvider clock)
{
    public async Task HandleAsync()
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var workspace = (await repository.LoadAsync()).RetainRecentPeriods(today);
        var file = new DownloadFile(FormattableString.Invariant($"flexisheet-backup-{today:yyyy-MM-dd}.json"),
            codec.Serialize(workspace));
        await delivery.DownloadAsync(file);
    }
}
