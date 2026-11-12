using FlexiSheet.Application;
using FlexiSheet.Application.Abstractions;
using FlexiSheet.Domain;
using RecordHandler = FlexiSheet.Application.RecordSession.Handler;
using RecordRequest = FlexiSheet.Application.RecordSession.Request;
using DeleteHandler = FlexiSheet.Application.DeleteSession.Handler;
using DeleteRequest = FlexiSheet.Application.DeleteSession.Request;
using StartHandler = FlexiSheet.Application.StartFlexiPeriod.Handler;
using StartRequest = FlexiSheet.Application.StartFlexiPeriod.Request;
using AdjustHandler = FlexiSheet.Application.AdjustFlexiPeriod.Handler;
using AdjustRequest = FlexiSheet.Application.AdjustFlexiPeriod.Request;

namespace FlexiSheet.Tests;

public sealed class SessionRecordingTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly TimeProvider Clock = new RecordingClock();

    [Fact]
    public void MidnightRequiresAnExplicitEndOfDayBoundaryAndNeverWrapsToTheNextDay()
    {
        Assert.Throws<RuleViolationException>(() => Session(Monday, 23, 0));
        Assert.Throws<RuleViolationException>(() => Session(Monday, 9, 9));
        Assert.Throws<RuleViolationException>(() => new WorkSession(Guid.NewGuid(), Monday,
            new TimeOnly(23, 0), new TimeOnly(1, 0), true));

        var untilMidnight = new WorkSession(Guid.NewGuid(), Monday, new TimeOnly(23, 0), TimeOnly.MinValue, true);
        var afterMidnight = Session(Monday.AddDays(1), 0, 1);
        var period = Period([untilMidnight, afterMidnight]);
        Assert.Equal(TimeSpan.FromHours(1), untilMidnight.Duration);
        Assert.Equal(TimeSpan.FromHours(2), period.TotalWorkedTime);
        Assert.Equal(TimeSpan.FromDays(1), new WorkSession(Guid.NewGuid(), Monday,
            TimeOnly.MinValue, TimeOnly.MinValue, true).Duration);
    }

    [Fact]
    public void OverlapsAreRejectedIncludingContainedSessionsButAdjacentSessionsAreValid()
    {
        var morning = Session(Monday, 9, 12);
        Assert.Throws<RuleViolationException>(() => Period([morning, Session(Monday, 10, 11)]));
        Assert.Throws<RuleViolationException>(() => Period([morning, Session(Monday, 8, 10)]));
        Assert.Throws<RuleViolationException>(() => Period([morning, Session(Monday, 11, 13)]));

        var period = Period([Session(Monday, 12, 13), morning, Session(Monday.AddDays(1), 9, 12)]);
        Assert.Equal(TimeSpan.FromHours(7), period.TotalWorkedTime);
        Assert.Equal(new TimeOnly(9, 0), period.Sessions[0].StartTime);
    }

    [Fact]
    public void MoreThanTenSessionsInADayAreAllowedAndTotalsAreDerivedAtEveryLevel()
    {
        var sessions = Enumerable.Range(0, 12).Select(hour => Session(Monday, hour, hour + 1)).ToArray();
        var period = Period(sessions, TimeSpan.FromMinutes(-30));
        Assert.Equal(12, period.Days[0].Sessions.Count);
        Assert.Equal(TimeSpan.FromHours(12), period.Days[0].WorkedTime);
        Assert.Equal(TimeSpan.FromHours(4.5), period.Days[0].Balance);
        Assert.Equal(TimeSpan.FromHours(12), period.Weeks[0].WorkedTime);
        Assert.Equal(TimeSpan.FromHours(-25.5), period.Weeks[0].Balance);
        Assert.Equal(TimeSpan.FromHours(12), period.TotalWorkedTime);
        Assert.Equal(TimeSpan.FromHours(-138.5), period.Balance);
        Assert.Equal(4, period.Weeks.Count);
        Assert.All(period.Weeks, week => Assert.Equal(DayOfWeek.Monday, week.StartDate.DayOfWeek));
        Assert.Equal(28, period.Days.Count);
    }

    [Fact]
    public void SessionsOutsideThePeriodOrWithDuplicateIdentifiersAreRejected()
    {
        Assert.Throws<RuleViolationException>(() => Period([Session(Monday.AddDays(-1), 9, 10)]));
        Assert.Throws<RuleViolationException>(() => Period([Session(Monday.AddDays(28), 9, 10)]));
        var first = Session(Monday, 9, 10);
        var second = new WorkSession(first.Id, Monday.AddDays(1), new TimeOnly(9, 0), new TimeOnly(10, 0));
        Assert.Throws<RuleViolationException>(() => Period([first, second]));
        Assert.Throws<RuleViolationException>(() => new WorkSession(Guid.NewGuid(), Monday,
            new TimeOnly(9, 0, 1), new TimeOnly(10, 0)));
    }

    [Fact]
    public async Task RecordingEditingAndDeletingUpdateOnlyTheChosenPeriodAndPreserveExpectedTime()
    {
        var period = Period([]);
        var next = new FlexiPeriod(Guid.NewGuid(), Monday.AddDays(28), PeriodPlanningTests.NormalPattern(),
            TimeSpan.FromHours(20));
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(PeriodPlanningTests.NormalPattern()), [period, next]));
        var handler = new RecordHandler(repository);
        await handler.HandleAsync(new RecordRequest(period.Id, Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)));
        var saved = Assert.Single(repository.Workspace.Periods[0].Sessions);
        await handler.HandleAsync(new RecordRequest(period.Id, Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), SessionId: saved.Id));

        Assert.Equal(TimeSpan.FromHours(8), repository.Workspace.Periods[0].TotalWorkedTime);
        Assert.Equal(saved.Id, Assert.Single(repository.Workspace.Periods[0].Sessions).Id);
        Assert.Equal(period.ExpectedTime, repository.Workspace.Periods[0].ExpectedTime);
        Assert.Same(next, repository.Workspace.Periods[1]);
        await new DeleteHandler(repository).HandleAsync(new DeleteRequest(period.Id, saved.Id));
        Assert.Equal(TimeSpan.Zero, repository.Workspace.Periods[0].TotalWorkedTime);
        Assert.Equal(TimeSpan.FromHours(20), repository.Workspace.Periods[1].CarryOver);
    }

    [Fact]
    public async Task RejectedOverlapsAndUnknownSessionsDoNotWriteData()
    {
        var first = Session(Monday, 9, 12);
        var second = Session(Monday, 13, 17);
        var period = Period([first, second]);
        var repository = new MemoryRepository(new Workspace(null, [period]));
        var handler = new RecordHandler(repository);

        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(
            new RecordRequest(period.Id, Monday, new TimeOnly(11, 0), new TimeOnly(13, 0))));
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(
            new RecordRequest(period.Id, Monday, new TimeOnly(10, 0), new TimeOnly(14, 0), SessionId: second.Id)));
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(
            new RecordRequest(period.Id, Monday, new TimeOnly(8, 0), new TimeOnly(9, 0), SessionId: Guid.NewGuid())));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            new DeleteHandler(repository).HandleAsync(new DeleteRequest(period.Id, Guid.NewGuid())));
        Assert.Equal(0, repository.Saves);
        Assert.Same(period, repository.Workspace.Periods[0]);
    }

    [Fact]
    public async Task CalculatedCarryOverIsASnapshotAndManualOverridesDoNotCascade()
    {
        var first = Period([Session(Monday, 9, 17)], TimeSpan.FromHours(2));
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(PeriodPlanningTests.NormalPattern()), [first]));
        var handler = new StartHandler(repository, Clock);
        await handler.HandleAsync(new StartRequest(Monday.AddDays(28)));
        var second = repository.Workspace.Periods[1];
        Assert.Equal(TimeSpan.FromHours(-140), second.CarryOver);

        await new RecordHandler(repository).HandleAsync(new RecordRequest(first.Id, Monday.AddDays(1),
            new TimeOnly(9, 0), new TimeOnly(17, 0)));
        Assert.Equal(TimeSpan.FromHours(-132), repository.Workspace.Periods[0].Balance);
        Assert.Equal(TimeSpan.FromHours(-140), repository.Workspace.Periods[1].CarryOver);

        await new AdjustHandler(repository).HandleAsync(new AdjustRequest(first.Id, new TimeSpan[28], TimeSpan.FromHours(10)));
        Assert.Equal(2, repository.Workspace.Periods[0].Sessions.Count);
        Assert.Equal(TimeSpan.FromHours(26), repository.Workspace.Periods[0].Balance);
        Assert.Equal(TimeSpan.FromHours(-140), repository.Workspace.Periods[1].CarryOver);

        await new AdjustHandler(repository).HandleAsync(new AdjustRequest(second.Id, second.ExpectedTime, TimeSpan.FromHours(-3)));
        Assert.Equal(TimeSpan.FromHours(-3), repository.Workspace.Periods[1].CarryOver);
        await handler.HandleAsync(new StartRequest(Monday.AddDays(56), TimeSpan.FromHours(1000)));
        Assert.Equal(TimeSpan.FromHours(1000), repository.Workspace.Periods[2].CarryOver);
    }

    [Fact]
    public async Task MissingOrNonAdjacentPredecessorRequiresManualCarryOver()
    {
        var first = Period([]);
        var repository = new MemoryRepository(new Workspace(new WorkingPattern(PeriodPlanningTests.NormalPattern()), [first]));
        var handler = new StartHandler(repository, Clock);
        await Assert.ThrowsAsync<RuleViolationException>(() => handler.HandleAsync(new StartRequest(Monday.AddDays(56))));
        Assert.Equal(0, repository.Saves);
        await handler.HandleAsync(new StartRequest(Monday.AddDays(56), TimeSpan.Zero));
        Assert.Equal(2, repository.Workspace.Periods.Count);
    }

    private static WorkSession Session(DateOnly date, int startHour, int endHour) =>
        new(Guid.NewGuid(), date, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    private static FlexiPeriod Period(IEnumerable<WorkSession> sessions, TimeSpan carryOver = default) =>
        new(Guid.NewGuid(), Monday, PeriodPlanningTests.NormalPattern(), carryOver, sessions);

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

    private sealed class RecordingClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
