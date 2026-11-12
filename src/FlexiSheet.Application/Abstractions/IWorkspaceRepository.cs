namespace FlexiSheet.Application.Abstractions;

public interface IWorkspaceRepository
{
    Task<Workspace> LoadAsync();
    Task SaveAsync(Workspace workspace);
}
