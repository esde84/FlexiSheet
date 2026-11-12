using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;

namespace FlexiSheet.Infrastructure;

public sealed class WorkspaceFileCodec : IWorkspaceFileCodec
{
    public string Serialize(Workspace workspace) => WorkspaceJson.Serialize(workspace);
    public Workspace Deserialize(string json) => WorkspaceJson.Deserialize(json);
}
