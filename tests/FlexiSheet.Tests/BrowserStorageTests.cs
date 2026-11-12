using System.Text.Json.Nodes;
using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;
using FlexiSheet.Infrastructure;
using Microsoft.JSInterop;

namespace FlexiSheet.Tests;

public sealed class BrowserStorageTests
{
    [Fact]
    public void JsonRoundTripPreservesDatesAll28DaysAndNegativeCarryOver()
    {
        var period = new FlexiPeriod(Guid.NewGuid(), new DateOnly(2026, 11, 2),
            PeriodPlanningTests.NormalPattern(), TimeSpan.FromMinutes(-135));
        var original = new Workspace(new WorkingPattern(PeriodPlanningTests.NormalPattern()), [period]);
        var json = WorkspaceJson.Serialize(original);
        var restored = WorkspaceJson.Deserialize(json);

        Assert.Contains("\"schemaVersion\":2", json);
        Assert.Equal(original.WorkingPattern!.ExpectedTime, restored.WorkingPattern!.ExpectedTime);
        var restoredPeriod = Assert.Single(restored.Periods);
        Assert.Equal(period.Id, restoredPeriod.Id);
        Assert.Equal(period.StartDate, restoredPeriod.StartDate);
        Assert.Equal(period.ExpectedTime, restoredPeriod.ExpectedTime);
        Assert.Equal(period.CarryOver, restoredPeriod.CarryOver);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":3,\"workingPattern\":null,\"periods\":[]}")]
    [InlineData("{\"schemaVersion\":\"2\",\"workingPattern\":null,\"periods\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"workingPattern\":null,\"periods\":null}")]
    [InlineData("{\"schemaVersion\":1,\"workingPattern\":null,\"periods\":[null]}")]
    [InlineData("{\"schemaVersion\":1,\"workingPattern\":[\"00:00:00\"],\"periods\":[]}")]
    public void InvalidOrUnsupportedSavedDataIsRejected(string json)
    {
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(json));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("startDate")]
    [InlineData("expectedTime")]
    [InlineData("carryOver")]
    [InlineData("sessions")]
    public void MissingPeriodFieldsAreNotSilentlyDefaulted(string field)
    {
        var json = JsonNode.Parse(WorkspaceJson.Serialize(Example()))!;
        json["periods"]![0]!.AsObject().Remove(field);
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(json.ToJsonString()));
    }

    [Fact]
    public void InvalidAndOverlappingStoredPeriodsAreRejected()
    {
        var json = JsonNode.Parse(WorkspaceJson.Serialize(Example()))!;
        var copy = json["periods"]![0]!.DeepClone();
        copy["id"] = Guid.NewGuid().ToString();
        json["periods"]!.AsArray().Add(copy);
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(json.ToJsonString()));

        json["periods"]!.AsArray().RemoveAt(1);
        json["periods"]![0]!["startDate"] = "2026-11-03";
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(json.ToJsonString()));
    }

    [Fact]
    public void PlanningSchemaMigratesWithoutLosingExpectedTimeOrCarryOver()
    {
        var original = Example();
        var json = JsonNode.Parse(WorkspaceJson.Serialize(original))!;
        json["schemaVersion"] = 1;
        json["periods"]![0]!.AsObject().Remove("sessions");

        var restored = WorkspaceJson.Deserialize(json.ToJsonString());

        var period = Assert.Single(restored.Periods);
        Assert.Empty(period.Sessions);
        Assert.Equal(original.Periods[0].ExpectedTime, period.ExpectedTime);
        Assert.Equal(original.Periods[0].CarryOver, period.CarryOver);
        Assert.Contains("\"schemaVersion\":2", WorkspaceJson.Serialize(restored));
    }

    [Fact]
    public void SessionsRoundTripIncludingEndOfDayBoundaryAndInvalidOverlapIsRejected()
    {
        var day = new DateOnly(2026, 11, 2);
        var session = new WorkSession(Guid.NewGuid(), day, new TimeOnly(23, 0), TimeOnly.MinValue, true);
        var period = new FlexiPeriod(Guid.NewGuid(), day, PeriodPlanningTests.NormalPattern(), TimeSpan.Zero, [session]);
        var json = WorkspaceJson.Serialize(new Workspace(null, [period]));
        var restored = Assert.Single(WorkspaceJson.Deserialize(json).Periods).Sessions.Single();
        Assert.Equal(session.Date, restored.Date);
        Assert.Equal(session.StartTime, restored.StartTime);
        Assert.True(restored.EndsAtMidnight);
        Assert.Equal(TimeSpan.FromHours(1), restored.Duration);

        var node = JsonNode.Parse(json)!;
        var duplicate = node["periods"]![0]!["sessions"]![0]!.DeepClone();
        duplicate["id"] = Guid.NewGuid().ToString();
        node["periods"]![0]!["sessions"]!.AsArray().Add(duplicate);
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(node.ToJsonString()));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("date")]
    [InlineData("startTime")]
    [InlineData("endTime")]
    [InlineData("endsAtMidnight")]
    public void IncompleteSessionsCannotBeSilentlyDefaulted(string field)
    {
        var day = new DateOnly(2026, 11, 2);
        var period = new FlexiPeriod(Guid.NewGuid(), day, PeriodPlanningTests.NormalPattern(), TimeSpan.Zero,
            [new WorkSession(Guid.NewGuid(), day, new TimeOnly(9, 0), new TimeOnly(10, 0))]);
        var node = JsonNode.Parse(WorkspaceJson.Serialize(new Workspace(null, [period])))!;
        node["periods"]![0]!["sessions"]![0]!.AsObject().Remove(field);
        Assert.Throws<WorkspaceStorageException>(() => WorkspaceJson.Deserialize(node.ToJsonString()));
    }

    [Fact]
    public async Task CorruptDataIsNotOverwrittenOnLoad()
    {
        var js = new StorageRuntime { Json = "invalid" };
        var repository = new BrowserWorkspaceRepository(js);
        await Assert.ThrowsAsync<WorkspaceStorageException>(() => repository.LoadAsync());
        Assert.Equal("invalid", js.Json);
        Assert.Equal(0, js.Writes);
    }

    [Fact]
    public async Task RepositorySavesAndReloadsOnlyAfterSuccessfulStorageWrite()
    {
        var js = new StorageRuntime();
        var repository = new BrowserWorkspaceRepository(js);
        Assert.Empty((await repository.LoadAsync()).Periods);
        await repository.SaveAsync(Example());
        Assert.Single((await repository.LoadAsync()).Periods);
        Assert.Equal(1, js.Writes);
    }

    [Fact]
    public async Task ChangesInAnotherTabAreDetectedBeforeLoadingOrSaving()
    {
        var js = new StorageRuntime();
        var repository = new BrowserWorkspaceRepository(js);
        await repository.LoadAsync();
        js.Json = WorkspaceJson.Serialize(Example());

        await Assert.ThrowsAsync<WorkspaceStorageException>(() => repository.LoadAsync());
        await Assert.ThrowsAsync<WorkspaceStorageException>(() => repository.SaveAsync(Workspace.Empty));
        Assert.Single(WorkspaceJson.Deserialize(js.Json).Periods);
        Assert.Equal(0, js.Writes);
    }

    [Fact]
    public async Task StorageFailureIsReportedWithoutClaimingASave()
    {
        var js = new StorageRuntime();
        var repository = new BrowserWorkspaceRepository(js);
        await repository.LoadAsync();
        js.FailWrites = true;
        var exception = await Assert.ThrowsAsync<WorkspaceStorageException>(() => repository.SaveAsync(Example()));
        Assert.Contains("could not be saved", exception.Message);
        Assert.Null(js.Json);
    }

    private static Workspace Example() => new(new WorkingPattern(PeriodPlanningTests.NormalPattern()),
        [PeriodPlanningTests.Period(new DateOnly(2026, 11, 2))]);

    private sealed class StorageRuntime : IJSRuntime
    {
        public string? Json { get; set; }
        public int Writes { get; private set; }
        public bool FailWrites { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? result;
            if (identifier == "flexiSheetStorage.read")
                result = Json;
            else if (identifier == "flexiSheetStorage.write")
            {
                if (FailWrites) throw new JSException("QuotaExceededError");
                var matches = Json == (string?)args![0];
                if (matches)
                {
                    Json = (string)args[1]!;
                    Writes++;
                }
                result = matches;
            }
            else
                throw new InvalidOperationException(identifier);

            return ValueTask.FromResult((TValue)result!);
        }
    }
}
