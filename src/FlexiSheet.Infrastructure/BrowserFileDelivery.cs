using FlexiSheet.Application.Abstractions;
using Microsoft.JSInterop;

namespace FlexiSheet.Infrastructure;

public sealed class BrowserFileDelivery(IJSRuntime javascript) : IFileDelivery
{
    public async Task DownloadAsync(DownloadFile file)
    {
        try
        {
            await javascript.InvokeVoidAsync("flexiSheetFiles.download", file.FileName, file.Json);
        }
        catch (JSException exception)
        {
            throw new WorkspaceStorageException(
                "The file download could not be started. Check browser download permissions and try again.", exception);
        }
    }

    public async Task ComposeEmailAsync(string mailto)
    {
        try
        {
            await javascript.InvokeVoidAsync("flexiSheetFiles.email", mailto);
        }
        catch (JSException exception)
        {
            throw new WorkspaceStorageException(
                "The file download was requested, but the email app could not be opened. Attach the downloaded file in your usual email app.", exception);
        }
    }
}
