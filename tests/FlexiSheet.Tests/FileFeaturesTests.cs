using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;
using FlexiSheet.Infrastructure;
using ExportHandler = FlexiSheet.Application.ExportBackup.Handler;
using PrepareHandler = FlexiSheet.Application.PrepareRestore.Handler;
using PrepareRequest = FlexiSheet.Application.PrepareRestore.Request;
using RestoreHandler = FlexiSheet.Application.RestoreBackup.Handler;
using RestoreRequest = FlexiSheet.Application.RestoreBackup.Request;
using DownloadHandler = FlexiSheet.Application.DownloadFlexiPeriod.Handler;
using DownloadRequest = FlexiSheet.Application.DownloadFlexiPeriod.Request;
using EmailHandler = FlexiSheet.Application.EmailFlexiPeriod.Handler;
using EmailRequest = FlexiSheet.Application.EmailFlexiPeriod.Request;

namespace FlexiSheet.Tests;

public sealed class FileFeaturesTests
{
    private static readonly TimeProvider Clock = new FixedClock(new DateOnly(2026, 11, 10));
    private static readonly IWorkspaceFileCodec Codec = new WorkspaceFileCodec();

    [Fact]
    public async Task BackupIncludesThePatternAndAllRetainedPeriodsWithoutChangingStoredData()
    {
        var source = ImportSource();
        var repository = new MemoryRepository(source);
        var delivery = new Delivery();

        await new ExportHandler(repository, Codec, delivery, Clock).HandleAsync();

        var file = Assert.Single(delivery.Files);
        Assert.Equal("flexisheet-backup-2026-11-10.json", file.FileName);
        var exported = Codec.Deserialize(file.Json);
        Assert.Equal(2, exported.Periods.Count);
        Assert.Equal(source.WorkingPattern!.ExpectedTime, exported.WorkingPattern!.ExpectedTime);
        Assert.Contains(exported.Periods, period => period.EndDate == new DateOnly(2024, 11, 10));
        Assert.Equal(source.Periods[2].Sessions[0].Duration, exported.Periods[1].Sessions[0].Duration);
        Assert.Equal(TimeSpan.FromHours(-2), exported.Periods[1].CarryOver);
        Assert.Equal(0, repository.Saves);
        Assert.Same(source, repository.Workspace);
        Assert.Null(delivery.Mailto);
    }

    [Fact]
    public async Task RestoreRequiresConfirmationAndRevalidatesBeforeReplacingAnything()
    {
        var existing = new Workspace(new WorkingPattern(new TimeSpan[28]),
            [PeriodPlanningTests.Period(new DateOnly(2026, 11, 2))]);
        var repository = new MemoryRepository(existing);
        var handler = new RestoreHandler(repository, Codec, Clock);
        var json = Codec.Serialize(ImportSource());
        var preview = new PrepareHandler(Codec, Clock).Handle(new PrepareRequest(json));
        Assert.Equal(2, preview.RetainedPeriods);
        Assert.Equal(1, preview.ExpiredPeriods);
        Assert.True(preview.HasWorkingPattern);

        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(new RestoreRequest(json, false)));
        await Assert.ThrowsAsync<WorkspaceStorageException>(() => handler.HandleAsync(new RestoreRequest("invalid", true)));
        Assert.Same(existing, repository.Workspace);
        Assert.Equal(0, repository.Saves);

        var restored = await handler.HandleAsync(new RestoreRequest(preview.Json, true));
        Assert.Equal(1, repository.Saves);
        Assert.Equal(2, restored.Periods.Count);
        Assert.DoesNotContain(restored.Periods, period => period.Id == existing.Periods[0].Id);
        Assert.Equal(ImportSource().WorkingPattern!.ExpectedTime, restored.WorkingPattern!.ExpectedTime);
        Assert.Equal(TimeSpan.FromHours(-2), restored.Periods[1].CarryOver);
        Assert.True(Assert.Single(restored.Periods[1].Sessions).EndsAtMidnight);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":99,\"workingPattern\":null,\"periods\":[]}")]
    public void InvalidBackupCannotProduceAReplacementPreview(string json)
    {
        Assert.Throws<WorkspaceStorageException>(() => new PrepareHandler(Codec, Clock).Handle(new PrepareRequest(json)));
    }

    [Fact]
    public async Task FailedRestoreWriteDoesNotChangeTheExistingWorkspace()
    {
        var existing = Workspace.Empty;
        var repository = new MemoryRepository(existing) { FailWrites = true };
        await Assert.ThrowsAsync<WorkspaceStorageException>(() =>
            new RestoreHandler(repository, Codec, Clock).HandleAsync(new RestoreRequest(Codec.Serialize(ImportSource()), true)));
        Assert.Same(existing, repository.Workspace);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task StandaloneDownloadContainsOnlyTheChosenCompletedPeriod()
    {
        var source = ImportSource();
        var repository = new MemoryRepository(source);
        var delivery = new Delivery();
        await new DownloadHandler(repository, Codec, delivery, Clock).HandleAsync(new DownloadRequest(source.Periods[2].Id));

        var file = Assert.Single(delivery.Files);
        Assert.Equal("flexisheet-period-2026-10-05.json", file.FileName);
        var exported = Codec.Deserialize(file.Json);
        Assert.Null(exported.WorkingPattern);
        Assert.Equal(source.Periods[2].Id, Assert.Single(exported.Periods).Id);
        Assert.Equal(source.Periods[2].ExpectedTime, exported.Periods[0].ExpectedTime);
        Assert.Equal(source.Periods[2].Balance, exported.Periods[0].Balance);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task PeriodEndingTodayCannotBeSharedUntilTheNextLocalDay()
    {
        var period = Completed();
        var repository = new MemoryRepository(new Workspace(null, [period]));
        var delivery = new Delivery();
        var today = new FixedClock(period.EndDate);
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new DownloadHandler(repository, Codec, delivery, today).HandleAsync(new DownloadRequest(period.Id)));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new EmailHandler(repository, Codec, delivery, today).HandleAsync(new EmailRequest(period.Id)));
        Assert.Empty(delivery.Events);

        await new DownloadHandler(repository, Codec, delivery, new FixedClock(period.EndDate.AddDays(1)))
            .HandleAsync(new DownloadRequest(period.Id));
        Assert.Single(delivery.Files);
    }

    [Fact]
    public async Task ActiveAndMissingPeriodsCannotBeShared()
    {
        var active = PeriodPlanningTests.Period(new DateOnly(2026, 11, 2));
        var repository = new MemoryRepository(new Workspace(null, [active]));
        var delivery = new Delivery();
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new DownloadHandler(repository, Codec, delivery, Clock).HandleAsync(new DownloadRequest(active.Id)));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new EmailHandler(repository, Codec, delivery, Clock).HandleAsync(new EmailRequest(Guid.NewGuid())));
        Assert.Empty(delivery.Events);
    }

    [Fact]
    public async Task EmailDownloadsFirstAndEncodesAShortSummaryAndManualAttachmentInstruction()
    {
        var period = Completed();
        var repository = new MemoryRepository(new Workspace(null, [period]));
        var delivery = new Delivery();
        await new EmailHandler(repository, Codec, delivery, Clock).HandleAsync(new EmailRequest(period.Id));

        Assert.Equal(["download", "email"], delivery.Events);
        Assert.Single(delivery.Files);
        Assert.StartsWith("mailto:?subject=", delivery.Mailto);
        Assert.DoesNotContain("attachment=", delivery.Mailto);
        var decoded = Uri.UnescapeDataString(delivery.Mailto!);
        Assert.Contains("Flexi sheet for period starting 2026-10-05", decoded);
        Assert.Contains("2026-10-05 to 2026-11-01", decoded);
        Assert.Contains("Worked: 1:00", decoded);
        Assert.Contains("Expected: 150:00", decoded);
        Assert.Contains("Carry-over: -2:00", decoded);
        Assert.Contains("Balance: -151:00", decoded);
        Assert.Contains("Please attach the downloaded file flexisheet-period-2026-10-05.json before sending.", decoded);
        Assert.True(delivery.Mailto!.Length < 1500);
    }

    [Fact]
    public async Task FailedDownloadDoesNotOpenAnEmailDraft()
    {
        var period = Completed();
        var delivery = new Delivery { FailDownload = true };
        await Assert.ThrowsAsync<WorkspaceStorageException>(() =>
            new EmailHandler(new MemoryRepository(new Workspace(null, [period])), Codec, delivery, Clock)
                .HandleAsync(new EmailRequest(period.Id)));
        Assert.Equal(["download"], delivery.Events);
        Assert.Null(delivery.Mailto);
    }

    private static FlexiPeriod Completed()
    {
        var start = new DateOnly(2026, 10, 5);
        return new FlexiPeriod(Guid.NewGuid(), start, PeriodPlanningTests.NormalPattern(), TimeSpan.FromHours(-2),
            [new WorkSession(Guid.NewGuid(), start, new TimeOnly(23, 0), TimeOnly.MinValue, true)]);
    }

    private static Workspace ImportSource() => new(new WorkingPattern(PeriodPlanningTests.NormalPattern()),
        [
            PeriodPlanningTests.Period(new DateOnly(2024, 9, 16)),
            PeriodPlanningTests.Period(new DateOnly(2024, 10, 14)),
            Completed()
        ]);

    private sealed class FixedClock(DateOnly date) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class MemoryRepository(Workspace workspace) : IWorkspaceRepository
    {
        public Workspace Workspace { get; private set; } = workspace;
        public int Saves { get; private set; }
        public bool FailWrites { get; init; }
        public Task<Workspace> LoadAsync() => Task.FromResult(Workspace);
        public Task SaveAsync(Workspace value)
        {
            if (FailWrites) throw new WorkspaceStorageException("Storage is full.");
            Workspace = value;
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class Delivery : IFileDelivery
    {
        public List<string> Events { get; } = [];
        public List<DownloadFile> Files { get; } = [];
        public string? Mailto { get; private set; }
        public bool FailDownload { get; init; }
        public Task DownloadAsync(DownloadFile file)
        {
            Events.Add("download");
            if (FailDownload) throw new WorkspaceStorageException("Downloads are disabled.");
            Files.Add(file);
            return Task.CompletedTask;
        }
        public Task ComposeEmailAsync(string mailto)
        {
            Events.Add("email");
            Mailto = mailto;
            return Task.CompletedTask;
        }
    }
}
