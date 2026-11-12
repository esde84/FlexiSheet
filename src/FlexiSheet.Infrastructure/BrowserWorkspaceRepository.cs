using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using Microsoft.JSInterop;

namespace FlexiSheet.Infrastructure;

public sealed class BrowserWorkspaceRepository(IJSRuntime javascript) : IWorkspaceRepository
{
    private string? loadedJson;
    private bool loaded;

    public async Task<Workspace> LoadAsync()
    {
        try
        {
            var json = await javascript.InvokeAsync<string?>("flexiSheetStorage.read");
            if (loaded && json != loadedJson)
                throw new WorkspaceStorageException(
                    "Another tab changed your data. Reload this page before trying again. Your changes were not saved.");
            // Confirmed recovery must still compare against the original revision if decoding fails.
            loadedJson = json;
            loaded = true;
            return json is null ? Workspace.Empty : WorkspaceJson.Deserialize(json);
        }
        catch (JSException exception)
        {
            throw new WorkspaceStorageException(
                "Browser storage is unavailable. Enable storage for this site and reload. Nothing was saved.", exception);
        }
    }

    public async Task SaveAsync(Workspace workspace)
    {
        if (!loaded)
            throw new WorkspaceStorageException("Browser storage has not been opened. Retry loading the app before saving or restoring. Nothing has been changed.");

        var json = WorkspaceJson.Serialize(workspace);
        try
        {
            var saved = await javascript.InvokeAsync<bool>("flexiSheetStorage.write", loadedJson, json);
            if (!saved)
                throw new WorkspaceStorageException(
                    "Another tab changed your data. Reload this page before trying again. Your changes were not saved.");
            loadedJson = json;
        }
        catch (JSException exception)
        {
            throw new WorkspaceStorageException(
                "Your changes could not be saved. Browser storage may be full or disabled. Keep this page open and try again.", exception);
        }
    }
}
