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
            new() { Level = 1, RequiredXp = 20, Name = "Huevito", AvatarUrl = "https://cdn.example.com/huevito.png" },
            new() { Level = 2, RequiredXp = 60, Name = "Pollito", AvatarUrl = "https://cdn.example.com/pollito.png" },
            new() { Level = 3, RequiredXp = 120, Name = "Gallina", AvatarUrl = "https://cdn.example.com/gallina.png" },
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
                new() { Level = 1, RequiredXp = 10, Name = "Huevito" },
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
    public void CurrentLevelMetadata_WhenConfigRemoved_ReturnsNullNameAndUrl()
    {
        // Create config without metadata for level 1
        var options = new GamificationOptions
        {
            XpPerCompletedTask = 20,
            Levels = new List<GamificationLevelOptions>
            {
                new() { Level = 1, RequiredXp = 20, Name = null, AvatarUrl = null },
            }
        };
        var service = CreateService(options);
        var metadata = service.GetLevelMetadata(1);
        Assert.NotNull(metadata);
        Assert.Equal(1, metadata!.Level);
        Assert.Null(metadata.Name);
        Assert.Null(metadata.AvatarUrl);
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
    public void GetLevelMetadata_ForExistingLevel_ReturnsConfiguredMetadata()
    {
        var service = CreateService();
        var metadata = service.GetLevelMetadata(2);
        Assert.NotNull(metadata);
        Assert.Equal(2, metadata!.Level);
        Assert.Equal("Pollito", metadata.Name);
        Assert.Equal("https://cdn.example.com/pollito.png", metadata.AvatarUrl);
    }

    [Fact]
    public void GetLevelMetadata_ForDuplicateLevel_UsesLastConfiguredValue()
    {
        var options = new GamificationOptions
        {
            XpPerCompletedTask = 20,
            Levels = new List<GamificationLevelOptions>
            {
                new() { Level = 1, RequiredXp = 20, Name = "Old", AvatarUrl = "old.png" },
                new() { Level = 1, RequiredXp = 20, Name = "New", AvatarUrl = "new.png" },
            }
        };
        var service = CreateService(options);

        var metadata = service.GetLevelMetadata(1);

        Assert.NotNull(metadata);
        Assert.Equal("New", metadata!.Name);
        Assert.Equal("new.png", metadata.AvatarUrl);
    }

    [Fact]
    public void GetLevelMetadata_ForNonExistentLevel_ReturnsNull()
    {
        var service = CreateService();
        var metadata = service.GetLevelMetadata(99);
        Assert.Null(metadata);
    }
}
