using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;
using StartHandler = FlexiSheet.Application.StartFlexiPeriod.Handler;
using StartRequest = FlexiSheet.Application.StartFlexiPeriod.Request;
using PatternHandler = FlexiSheet.Application.SaveWorkingPattern.Handler;
using PatternRequest = FlexiSheet.Application.SaveWorkingPattern.Request;
using AdjustHandler = FlexiSheet.Application.AdjustFlexiPeriod.Handler;
using AdjustRequest = FlexiSheet.Application.AdjustFlexiPeriod.Request;
using LoadHandler = FlexiSheet.Application.LoadWorkspace.Handler;

namespace FlexiSheet.Tests;

public sealed class PeriodPlanningTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 11, 12, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(27)]
    [InlineData(29)]
    public void PatternRequiresExactlyFourWeeks(int count)
    {
        Assert.Throws<RuleViolationException>(() => new WorkingPattern(new TimeSpan[count]));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void DailyExpectedTimeMustFitTheDay(int minutes)
    {
        var days = new TimeSpan[28];
        days[0] = TimeSpan.FromMinutes(minutes);
        Assert.Throws<RuleViolationException>(() => new WorkingPattern(days));
    }

    [Fact]
    public void PatternPreservesMinutePrecisionAndDoesNotAliasInput()
    {
        var days = NormalPattern();
        var pattern = new WorkingPattern(days);
        days[0] = TimeSpan.Zero;

        Assert.Equal(TimeSpan.FromMinutes(450), pattern.ExpectedTime[0]);
        Assert.Equal(TimeSpan.FromHours(150), pattern.TotalExpectedTime);
        Assert.Throws<RuleViolationException>(() => new WorkingPattern(
            Enumerable.Repeat(TimeSpan.FromSeconds(1), 28)));
    }

    [Fact]
    public void PeriodMustStartOnMondayAndEndsOnTheFourthSunday()
    {
        var period = Period(Monday);
        Assert.Equal(new DateOnly(2026, 11, 29), period.EndDate);
        Assert.Throws<RuleViolationException>(() => Period(Monday.AddDays(1)));
    }

    [Theory]
    [InlineData(-10000)]
    [InlineData(10000)]
    public void CarryOverMayBePositiveOrNegativeWithoutABusinessCap(int hours)
    {
        var period = new FlexiPeriod(Guid.NewGuid(), Monday, NormalPattern(), TimeSpan.FromHours(hours));
        Assert.Equal(TimeSpan.FromHours(hours), period.CarryOver);
    }

    [Fact]
    public async Task StartingAPeriodRequiresASavedPattern()
    {
        var repository = new MemoryRepository(Workspace.Empty);
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new StartHandler(repository, Clock).HandleAsync(new StartRequest(Monday, TimeSpan.Zero)));
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task NewPeriodsSnapshotThePatternWhileLaterSettingsDoNotChangeThem()
    {
        var repository = new MemoryRepository(Workspace.Empty);
        await new PatternHandler(repository).HandleAsync(new PatternRequest(NormalPattern()));
        await new StartHandler(repository, Clock).HandleAsync(new StartRequest(Monday, TimeSpan.FromMinutes(-135)));
        await new PatternHandler(repository).HandleAsync(new PatternRequest(new TimeSpan[28]));

        var period = Assert.Single(repository.Workspace.Periods);
        Assert.Equal(TimeSpan.FromHours(150), period.TotalExpectedTime);
        Assert.Equal(TimeSpan.FromMinutes(-135), period.CarryOver);
        Assert.Equal(TimeSpan.Zero, repository.Workspace.WorkingPattern!.TotalExpectedTime);
    }

    [Fact]
    public async Task OverlappingPeriodIsRejectedButAdjacentPeriodIsAllowed()
    {
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(NormalPattern()), [Period(Monday)]));
        var handler = new StartHandler(repository, Clock);

        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(new StartRequest(Monday, TimeSpan.Zero)));
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(new StartRequest(Monday.AddDays(7), TimeSpan.Zero)));
        Assert.Equal(0, repository.Saves);

        await handler.HandleAsync(new StartRequest(Monday.AddDays(28), TimeSpan.Zero));
        Assert.Equal(2, repository.Workspace.Periods.Count);
    }

    [Fact]
    public async Task AdjustingAPeriodChangesOnlyThatPeriodsExpectedTimeAndCarryOver()
    {
        var first = Period(Monday);
        var second = Period(Monday.AddDays(28));
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(NormalPattern()), [first, second]));
        var adjusted = NormalPattern();
        adjusted[0] = TimeSpan.FromHours(3);

        await new AdjustHandler(repository).HandleAsync(new AdjustRequest(first.Id, adjusted, TimeSpan.FromHours(200)));

        var changed = repository.Workspace.Periods.Single(period => period.Id == first.Id);
        Assert.Equal(TimeSpan.FromHours(3), changed.ExpectedTime[0]);
        Assert.Equal(TimeSpan.FromHours(200), changed.CarryOver);
        Assert.Equal(TimeSpan.FromHours(7.5), repository.Workspace.WorkingPattern!.ExpectedTime[0]);
        Assert.Same(second, repository.Workspace.Periods.Single(period => period.Id == second.Id));
    }

    [Fact]
    public async Task InvalidAdjustmentAndMissingPeriodLeaveStoredDataUntouched()
    {
        var period = Period(Monday);
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(NormalPattern()), [period]));
        var handler = new AdjustHandler(repository);
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(
            new AdjustRequest(period.Id, new TimeSpan[27], TimeSpan.Zero)));
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(
            new AdjustRequest(Guid.NewGuid(), NormalPattern(), TimeSpan.Zero)));
        Assert.Equal(0, repository.Saves);
        Assert.Same(period, Assert.Single(repository.Workspace.Periods));
    }

    [Fact]
    public async Task StartupPurgesOnlyPeriodsEndingStrictlyBeforeTheTwoYearCutoff()
    {
        // The cutoff is a Sunday so a period can end exactly on the boundary.
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        var expired = Period(new DateOnly(2024, 9, 16));
        var boundary = Period(new DateOnly(2024, 10, 14));
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(NormalPattern()), [expired, boundary]));

        var loaded = await new LoadHandler(repository, clock).HandleAsync();
        Assert.Same(boundary, Assert.Single(loaded.Periods));
        Assert.Equal(new DateOnly(2024, 11, 10), boundary.EndDate);
        Assert.Equal(1, repository.Saves);
        await new LoadHandler(repository, clock).HandleAsync();
        Assert.Equal(1, repository.Saves);
    }

    [Fact]
    public async Task StartingAlreadyExpiredPeriodIsRejected()
    {
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(NormalPattern()), []));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new StartHandler(repository, Clock).HandleAsync(new StartRequest(new DateOnly(2020, 1, 6), TimeSpan.Zero)));
        Assert.Equal(0, repository.Saves);
    }

    internal static TimeSpan[] NormalPattern() => Enumerable.Range(0, 28)
        .Select(index => index % 7 < 5 ? TimeSpan.FromMinutes(450) : TimeSpan.Zero).ToArray();

    internal static FlexiPeriod Period(DateOnly start) => new(Guid.NewGuid(), start, NormalPattern(), TimeSpan.Zero);

    private sealed class MemoryRepository(Workspace workspace) : IWorkspaceRepository
    {
        public Workspace Workspace { get; private set; } = workspace;
        public int Saves { get; private set; }
        public Task<Workspace> LoadAsync() => Task.FromResult(Workspace);
        public Task SaveAsync(Workspace value)
        {
            Workspace = value;
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
