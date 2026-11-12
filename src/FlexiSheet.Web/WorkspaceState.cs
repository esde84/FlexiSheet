using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Web;

public sealed class WorkspaceState(ILogger<WorkspaceState> logger)
{
    public Workspace? Current { get; private set; }
    public string? Error { get; private set; }
    public string? Success { get; private set; }

    public void ClearMessages() => (Error, Success) = (null, null);

    public Task<bool> ExecuteAsync(Func<Task<Workspace>> action, string? success = null) =>
        PerformAsync(async () => { Current = await action(); }, success);

    public async Task<bool> PerformAsync(Func<Task> action, string? success = null)
    {
        ClearMessages();
        try
        {
            await action();
            Success = success;
            return true;
        }
        catch (Exception exception) when (exception is RuleViolationException or WorkspaceStorageException)
        {
            logger.LogWarning(exception, "FlexiSheet operation did not complete");
            Error = exception.Message;
            return false;
        }
    }
}
