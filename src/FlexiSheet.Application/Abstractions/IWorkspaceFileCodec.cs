namespace FlexiSheet.Application.Abstractions;

public interface IWorkspaceFileCodec
{
    string Serialize(Workspace workspace);
    Workspace Deserialize(string json);
}
