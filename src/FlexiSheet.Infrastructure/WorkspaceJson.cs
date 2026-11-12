using System.Text.Json;
using System.Text.Json.Serialization;
using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;

namespace FlexiSheet.Infrastructure;

public static class WorkspaceJson
{
    private const int SchemaVersion = 2;

    public static string Serialize(Workspace workspace)
    {
        workspace.Validate();
        var document = new WorkspaceDocument(SchemaVersion, workspace.WorkingPattern?.ExpectedTime.ToArray(),
            workspace.Periods.Select(period => new PeriodDocument(
                period.Id, period.StartDate, period.ExpectedTime.ToArray(), period.CarryOver,
                period.Sessions.Select(session => new SessionDocument(session.Id, session.Date,
                    session.StartTime, session.EndTime, session.EndsAtMidnight)).ToArray())).ToArray());
        return JsonSerializer.Serialize(document, WorkspaceJsonContext.Default.WorkspaceDocument);
    }

    public static Workspace Deserialize(string json)
    {
        try
        {
            using var parsed = JsonDocument.Parse(json);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object ||
                !parsed.RootElement.TryGetProperty("schemaVersion", out var version) ||
                version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out var schemaVersion))
                throw new WorkspaceStorageException("Saved data has no valid schema version. Existing browser data has not been changed.");

            WorkspaceDocument? document;
            if (schemaVersion == 1)
            {
                var legacy = parsed.RootElement.Deserialize(WorkspaceJsonContext.Default.PlanningDocument)
                    ?? throw new WorkspaceStorageException("Saved data is empty. Existing browser data has not been changed.");
                document = new WorkspaceDocument(SchemaVersion, legacy.WorkingPattern,
                    legacy.Periods?.Select(period => period is null ? null : new PeriodDocument(
                        period.Id, period.StartDate, period.ExpectedTime, period.CarryOver, [])).ToArray());
            }
            else if (schemaVersion == SchemaVersion)
                document = parsed.RootElement.Deserialize(WorkspaceJsonContext.Default.WorkspaceDocument);
            else
                throw new WorkspaceStorageException("This saved-data version is not supported. Existing browser data has not been changed.");

            if (document?.Periods is null ||
                document.Periods.Any(period => period?.ExpectedTime is null || period.Sessions is null ||
                    period.Sessions.Any(session => session is null)))
                throw new WorkspaceStorageException("Saved flexi periods are incomplete. Existing browser data has not been changed.");

            var workspace = new Workspace(
                document.WorkingPattern is null ? null : new WorkingPattern(document.WorkingPattern),
                document.Periods.Select(period => new FlexiPeriod(
                    period!.Id, period.StartDate, period.ExpectedTime!, period.CarryOver,
                    period.Sessions!.Select(session => new WorkSession(session!.Id, session.Date,
                        session.StartTime, session.EndTime, session.EndsAtMidnight)))).ToArray());
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
    [property: JsonRequired] TimeSpan CarryOver,
    [property: JsonRequired] SessionDocument?[]? Sessions);

internal sealed record SessionDocument(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] DateOnly Date,
    [property: JsonRequired] TimeOnly StartTime,
    [property: JsonRequired] TimeOnly EndTime,
    [property: JsonRequired] bool EndsAtMidnight);

internal sealed record PlanningDocument(
    [property: JsonRequired] int SchemaVersion,
    [property: JsonRequired] TimeSpan[]? WorkingPattern,
    [property: JsonRequired] PlanningPeriodDocument?[]? Periods);

internal sealed record PlanningPeriodDocument(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] DateOnly StartDate,
    [property: JsonRequired] TimeSpan[]? ExpectedTime,
    [property: JsonRequired] TimeSpan CarryOver);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(WorkspaceDocument))]
[JsonSerializable(typeof(PlanningDocument))]
internal partial class WorkspaceJsonContext : JsonSerializerContext;
