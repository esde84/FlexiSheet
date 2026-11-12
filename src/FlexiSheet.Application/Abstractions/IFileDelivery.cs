namespace FlexiSheet.Application.Abstractions;

public sealed record DownloadFile(string FileName, string Json);

public interface IFileDelivery
{
    Task DownloadAsync(DownloadFile file);
    Task ComposeEmailAsync(string mailto);
}
