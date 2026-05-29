using Nido.Application.Onboarding;

namespace Nido.Application.Tests.Onboarding;

public sealed class OnboardingWellnessTests
{
    [Fact]
    public async Task SaveWellness_WhenRestrictionsAndGoalsAreProvided_PersistsBothCollections()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            false,
            [new RestrictionInput("allergy", "nuts")],
            [new HouseholdGoalInput("Eat better", "More veggies")],
            null,
            null), CancellationToken.None);

        Assert.Single(repo.Restrictions);
        Assert.Single(repo.Goals);
    }

    [Fact]
    public async Task SaveWellness_WhenClientSendsForgedIds_ThrowsUnauthorizedAccessException()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            false,
            [],
            [],
            Guid.NewGuid(),
            null), CancellationToken.None));
    }

    [Fact]
    public async Task SaveWellness_WhenOnlyRestrictionsAreProvided_AllowsPartialSubmission()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            false,
            [new RestrictionInput("allergy", "gluten")],
            [],
            null,
            null), CancellationToken.None);

        Assert.Single(repo.Restrictions);
        Assert.Empty(repo.Goals);
    }

    [Fact]
    public async Task SaveWellness_WhenOnlyGoalsAreProvided_AllowsPartialSubmission()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            false,
            [],
            [new HouseholdGoalInput("Cook more", "3 homemade meals")],
            null,
            null), CancellationToken.None);

        Assert.Empty(repo.Restrictions);
        Assert.Single(repo.Goals);
    }

    private sealed class FakeOnboardingRepository : IOnboardingRepository
    {
        public Guid UsuarioId { get; } = Guid.NewGuid();
        public Guid HogarId { get; } = Guid.NewGuid();
        public List<RestrictionInput> Restrictions { get; } = [];
        public List<HouseholdGoalInput> Goals { get; } = [];

        public Task<bool> IsUserHouseholdMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceGoalsAsync(Guid hogarId, IReadOnlyList<HouseholdGoalInput> goals, CancellationToken cancellationToken) { Goals.AddRange(goals); return Task.CompletedTask; }
        public Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReplaceRestrictionsAsync(Guid usuarioId, IReadOnlyList<RestrictionInput> restrictions, CancellationToken cancellationToken) { Restrictions.AddRange(restrictions); return Task.CompletedTask; }
    }
}
