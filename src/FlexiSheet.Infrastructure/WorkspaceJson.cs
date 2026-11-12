using System.Text.Json;
using System.Text.Json.Serialization;
using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Infrastructure;

public static class WorkspaceJson
{
    private const int SchemaVersion = 1;

    public static string Serialize(Workspace workspace)
    {
        workspace.Validate();
        var document = new WorkspaceDocument(SchemaVersion, workspace.WorkingPattern?.ExpectedTime.ToArray(),
            workspace.Periods.Select(period => new PeriodDocument(
                period.Id, period.StartDate, period.ExpectedTime.ToArray(), period.CarryOver)).ToArray());
        return JsonSerializer.Serialize(document, WorkspaceJsonContext.Default.WorkspaceDocument);
    }

    public static Workspace Deserialize(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize(json, WorkspaceJsonContext.Default.WorkspaceDocument)
                ?? throw new WorkspaceStorageException("Saved data is empty. Existing browser data has not been changed.");
            if (document.SchemaVersion != SchemaVersion)
                throw new WorkspaceStorageException("This saved-data version is not supported. Existing browser data has not been changed.");
            if (document.Periods is null || document.Periods.Any(period => period?.ExpectedTime is null))
                throw new WorkspaceStorageException("Saved flexi periods are incomplete. Existing browser data has not been changed.");

            var workspace = new Workspace(
                document.WorkingPattern is null ? null : new WorkingPattern(document.WorkingPattern),
                document.Periods.Select(period => new FlexiPeriod(
                    period!.Id, period.StartDate, period.ExpectedTime!, period.CarryOver)).ToArray());
            workspace.Validate();
            return workspace;
        }
        catch (Exception exception) when (exception is JsonException or RuleViolationException)
        {
            throw new WorkspaceStorageException(
                "Saved data could not be read. Existing browser data has not been changed.", exception);
        }
    }

}

internal sealed record WorkspaceDocument(
    [property: JsonRequired] int SchemaVersion,
    [property: JsonRequired] TimeSpan[]? WorkingPattern,
    [property: JsonRequired] PeriodDocument?[]? Periods);

internal sealed record PeriodDocument(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] DateOnly StartDate,
    [property: JsonRequired] TimeSpan[]? ExpectedTime,
    [property: JsonRequired] TimeSpan CarryOver);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WorkspaceDocument))]
internal partial class WorkspaceJsonContext : JsonSerializerContext;
