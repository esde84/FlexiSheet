namespace FlexiSheet.Application.Abstractions;

public sealed class WorkspaceStorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
