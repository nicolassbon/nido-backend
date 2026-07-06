using Microsoft.Extensions.Options;
using Nido.Application.Gamificacion;

namespace Nido.Application.Tests.Gamificacion;

public sealed class GamificationUnlockMaterializerTests
{
    private static GamificationOptions DefaultOptions() => new()
    {
        XpPerCompletedTask = 20,
        Levels = new List<GamificationLevelOptions>
        {
            new() { Level = 1, RequiredXp = 20 },
            new() { Level = 2, RequiredXp = 60 },
            new() { Level = 3, RequiredXp = 120 },
            new() { Level = 4, RequiredXp = 200 },
            new() { Level = 5, RequiredXp = 300 },
        }
    };

    private static (IGamificationUnlockMaterializer, InMemoryGamificationRepository) CreateMaterializer(
        GamificationOptions? options = null,
        InMemoryGamificationRepository? repo = null)
    {
        var repository = repo ?? new InMemoryGamificationRepository();
        var rulesService = new GamificationRulesService(Options.Create(options ?? DefaultOptions()));
        var materializer = new GamificationUnlockMaterializer(rulesService, repository);
        return (materializer, repository);
    }

    private readonly Guid _usuarioId = Guid.NewGuid();

    [Fact]
    public async Task Materialize_FirstThresholdCrossing_InsertsOneRow()
    {
        var usuarioId = _usuarioId;
        var repo = new InMemoryGamificationRepository().WithCompletedCount(1); // 20 XP → level 1 eligible
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Single(newlyInserted);
        Assert.Contains(1, newlyInserted);
    }

    [Fact]
    public async Task Materialize_MultipleEligibleThresholds_InsertsOneEach()
    {
        var usuarioId = _usuarioId;
        var repo = new InMemoryGamificationRepository().WithCompletedCount(4); // 80 XP → levels 1,2 eligible
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Equal(2, newlyInserted.Count);
        Assert.Contains(1, newlyInserted);
        Assert.Contains(2, newlyInserted);
    }

    [Fact]
    public async Task Materialize_RolloutUserWithHistoricalTasksBeforeFirstRead_InsertsAllMissingEligibleLowerLevels()
    {
        var usuarioId = _usuarioId;
        // 10 completed tasks = 200 XP → levels 1,2,3,4 all eligible
        var repo = new InMemoryGamificationRepository().WithCompletedCount(10);
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Equal(4, newlyInserted.Count);
        Assert.Contains(1, newlyInserted);
        Assert.Contains(2, newlyInserted);
        Assert.Contains(3, newlyInserted);
        Assert.Contains(4, newlyInserted);
    }

    [Fact]
    public async Task Materialize_ConfigAddedEligibleLevel_InsertsExactlyOneRow()
    {
        var usuarioId = _usuarioId;
        // User already has levels 1 and 2 unlocked, has 100 XP (5 tasks)
        var repo = new InMemoryGamificationRepository()
            .WithCompletedCount(5)
            .WithUnlockedLevels(usuarioId, 1, 2);

        // Config now has a new level 3bis with threshold 50 (between 2 and 3)
        var options = new GamificationOptions
        {
            XpPerCompletedTask = 20,
            Levels = new List<GamificationLevelOptions>
            {
                new() { Level = 1, RequiredXp = 20 },
                new() { Level = 2, RequiredXp = 60 },
                new() { Level = 10, RequiredXp = 50 }, // config-added eligible level
                new() { Level = 3, RequiredXp = 120 },
            }
        };
        var (materializer, _) = CreateMaterializer(options: options, repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Single(newlyInserted);
        Assert.Contains(10, newlyInserted);
    }

    [Fact]
    public async Task Materialize_LoweredThresholdEligibleLevel_InsertsExactlyOneRow()
    {
        var usuarioId = _usuarioId;
        // User has level 1 unlocked, has 100 XP (5 tasks). Level 3 threshold was 200, now lowered to 80.
        var repo = new InMemoryGamificationRepository()
            .WithCompletedCount(5)
            .WithUnlockedLevels(usuarioId, 1);

        var options = new GamificationOptions
        {
            XpPerCompletedTask = 20,
            Levels = new List<GamificationLevelOptions>
            {
                new() { Level = 1, RequiredXp = 20 },
                new() { Level = 2, RequiredXp = 60 },
                new() { Level = 3, RequiredXp = 80 }, // lowered from 120 to 80
            }
        };
        var (materializer, _) = CreateMaterializer(options: options, repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        // Levels 2 (60) and 3 (80) are eligible, and neither is unlocked yet.
        Assert.Equal(2, newlyInserted.Count);
        Assert.Contains(2, newlyInserted);
        Assert.Contains(3, newlyInserted);
    }

    [Fact]
    public async Task Materialize_PartialExistingUnlocks_InsertsMissingLowerAndHigherEligible()
    {
        var usuarioId = _usuarioId;
        // User has only level 2 unlocked, has 160 XP (8 tasks) → levels 1,2,3 eligible
        var repo = new InMemoryGamificationRepository()
            .WithCompletedCount(8)
            .WithUnlockedLevels(usuarioId, 2);
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Equal(2, newlyInserted.Count);
        Assert.Contains(1, newlyInserted);
        Assert.Contains(3, newlyInserted);
    }

    [Fact]
    public async Task Materialize_AlreadyUnlockedLevel_InsertsNothing()
    {
        var usuarioId = _usuarioId;
        var repo = new InMemoryGamificationRepository()
            .WithCompletedCount(5) // 100 XP
            .WithUnlockedLevels(usuarioId, 1, 2, 3); // all eligible levels already unlocked
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        Assert.Empty(newlyInserted);
    }

    [Fact]
    public async Task Materialize_RepeatedRunWithoutStateChange_InsertsNothingAdditional()
    {
        var usuarioId = _usuarioId;
        var repo = new InMemoryGamificationRepository().WithCompletedCount(3); // 60 XP → levels 1,2
        var (materializer, _) = CreateMaterializer(repo: repo);

        var first = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);
        Assert.Equal(2, first.Count);

        // Second run with same state
        var second = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);
        Assert.Empty(second);
    }

    [Fact]
    public async Task Materialize_RepositoryThrowsUnexpectedError_Propagates()
    {
        var usuarioId = _usuarioId;
        var repo = new InMemoryGamificationRepository().WithCompletedCount(1)
            .WithUnexpectedInsertFailure();
        var (materializer, _) = CreateMaterializer(repo: repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None));
    }

    [Fact]
    public async Task Materialize_ReturnsOnlyNewlyInsertedRows_AsEvolutionSignals()
    {
        var usuarioId = _usuarioId;
        // 3 completed = 60 XP → levels 1 (20) and 2 (60) are eligible
        var repo = new InMemoryGamificationRepository()
            .WithCompletedCount(3)
            .WithUnlockedLevels(usuarioId, 1); // only level 1 pre-exists
        var (materializer, _) = CreateMaterializer(repo: repo);

        var newlyInserted = await materializer.MaterializeEligibleUnlocksAsync(usuarioId, CancellationToken.None);

        // Only level 2 should be returned as newly inserted
        Assert.Single(newlyInserted);
        Assert.Contains(2, newlyInserted);
        Assert.DoesNotContain(1, newlyInserted); // pre-existing, not an evolution signal
    }
}
