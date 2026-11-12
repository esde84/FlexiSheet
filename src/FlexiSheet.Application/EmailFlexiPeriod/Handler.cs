using FlexiSheet.Application.Abstractions;
using FlexiSheet.Application.Sharing;

namespace FlexiSheet.Application.EmailFlexiPeriod;

public sealed record Request(Guid PeriodId);

public sealed class Handler(IWorkspaceRepository repository, IWorkspaceFileCodec codec,
    IFileDelivery delivery, TimeProvider clock)
{
    public async Task HandleAsync(Request request)
    {
        var workspace = await repository.LoadAsync();
        var export = CompletedPeriodFile.Create(workspace, request.PeriodId,
            DateOnly.FromDateTime(clock.GetLocalNow().DateTime), codec);
        var period = export.Period;
        var subject = FormattableString.Invariant($"Flexi sheet for period starting {period.StartDate:yyyy-MM-dd}");
        var body = FormattableString.Invariant($"Flexi period {period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}") +
            $"\r\nWorked: {DurationFormatter.Format(period.TotalWorkedTime)}" +
            $"\r\nExpected: {DurationFormatter.Format(period.TotalExpectedTime)}" +
            $"\r\nCarry-over: {DurationFormatter.Format(period.CarryOver)}" +
            $"\r\nBalance: {DurationFormatter.Format(period.Balance)}" +
            $"\r\n\r\nPlease attach the downloaded file {export.File.FileName} before sending.";
        var mailto = $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

        await delivery.DownloadAsync(export.File);
        await delivery.ComposeEmailAsync(mailto);
    }
}
