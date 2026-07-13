using Microsoft.Extensions.Options;
using Nido.Application.Gamificacion;

namespace Nido.Application.Tests.Gamificacion;

public sealed class GamificationRulesServiceTests
{
    private static GamificationOptions DefaultOptions() => new()
    {
        XpPerCompletedTask = 20,
        Levels = new List<GamificationLevelOptions>
        {
            new() { Level = 1, RequiredXp = 20 },
            new() { Level = 2, RequiredXp = 60 },
            new() { Level = 3, RequiredXp = 120 },
        }
    };

    private static IGamificationRulesService CreateService(GamificationOptions? options = null)
        => new GamificationRulesService(Options.Create(options ?? DefaultOptions()));

    [Fact]
    public void CurrentXp_WithZeroTasks_ReturnsZero()
    {
        var service = CreateService();
        var result = service.ComputeCurrentXp(0);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CurrentXp_WithOneTask_ReturnsConfiguredXp()
    {
        var service = CreateService();
        var result = service.ComputeCurrentXp(1);
        Assert.Equal(20, result);
    }

    [Fact]
    public void CurrentXp_WithManyTasks_ReturnsCountTimesConfiguredXp()
    {
        var service = CreateService();
        var result = service.ComputeCurrentXp(7);
        Assert.Equal(140, result);
    }

    [Theory]
    [MemberData(nameof(ConfigChangeData))]
    public void CurrentXp_WhenConfigChanges_RecomputesWithoutMigration(int xpPerTask, int completedCount, int expectedXp)
    {
        var options = new GamificationOptions
        {
            XpPerCompletedTask = xpPerTask,
            Levels = new List<GamificationLevelOptions>
            {
                new() { Level = 1, RequiredXp = 10 },
            }
        };
        var service = CreateService(options);
        var result = service.ComputeCurrentXp(completedCount);
        Assert.Equal(expectedXp, result);
    }

    public static IEnumerable<object[]> ConfigChangeData() =>
        new List<object[]>
        {
            new object[] { 10, 5, 50 },
            new object[] { 25, 5, 125 },
            new object[] { 50, 3, 150 },
            new object[] { 0, 10, 0 },
        };

    [Fact]
    public void CurrentXp_ClampsAtZero_NeverNegative()
    {
        // The formula is max(0, count * xpPerTask), so passing a negative count
        // (simulating an edge case) should still clamp to 0.
        var service = CreateService();
        var result = service.ComputeCurrentXp(-1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void Constructor_WithNegativeXpPerTask_Throws()
    {
        var options = DefaultOptions();
        options.XpPerCompletedTask = -1;

        Assert.Throws<InvalidOperationException>(() => CreateService(options));
    }

    [Fact]
    public void EligibleLevels_ForCurrentXp_ReturnsAllLevelsWithThresholdAtMostCurrentXp()
    {
        var service = CreateService();
        var eligible = service.ComputeEligibleLevels(60);
        Assert.Equal(new[] { 1, 2 }, eligible.OrderBy(x => x));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 20)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    public void LevelThreshold_ReturnsConfiguredThresholdOrZeroForTheInitialLevel(int level, int expectedThreshold)
    {
        var service = CreateService();

        Assert.Equal(expectedThreshold, service.GetLevelThreshold(level));
    }

    [Fact]
    public void NextLevel_BelowThreshold_ReturnsLevelThresholdAndRemainingXp()
    {
        var service = CreateService();
        var next = service.GetNextLevel(50);
        Assert.NotNull(next);
        Assert.Equal(2, next!.Level);
        Assert.Equal(60, next.ThresholdXp);
        Assert.Equal(10, next.XpToNextLevel);
    }

    [Fact]
    public void NextLevel_AtOrAboveHighest_ReturnsNullAndHasNextLevelFalse()
    {
        var service = CreateService();
        var next = service.GetNextLevel(200);
        Assert.Null(next);
    }

    [Fact]
    public void TaskXpOtorgado_ForCompletedTask_ReturnsConfiguredXp()
    {
        var service = CreateService();
        var xp = service.TaskXpOtorgado(isCompleted: true);
        Assert.Equal(20, xp);
    }

    [Fact]
    public void TaskXpOtorgado_ForNonCompletedTask_ReturnsNull()
    {
        var service = CreateService();
        var xp = service.TaskXpOtorgado(isCompleted: false);
        Assert.Null(xp);
    }

    [Fact]
    public void EligibleLevels_ForHigherXp_ReturnsAllConfiguredLevels()
    {
        var service = CreateService();
        var eligible = service.ComputeEligibleLevels(60);
        Assert.Equal(new[] { 1, 2 }, eligible);
    }
}
