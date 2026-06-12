using Nido.Application.Onboarding;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Tests.Onboarding;

public sealed class OnboardingHouseholdTests
{
    [Fact]
    public async Task SaveHousehold_WhenUserIsNotHouseholdMember_ThrowsAccessDenied()
    {
        var repo = new RecordingOnboardingRepository { IsMember = false };
        var handler = new SaveHouseholdStepHandler(repo);

        await Assert.ThrowsAsync<HouseholdAccessDeniedException>(() => handler.Handle(new SaveHouseholdStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: true,
            Members: [],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None));

        Assert.Empty(repo.MembersGuardados);
        Assert.Empty(repo.MarkedSteps);
    }

    [Fact]
    public async Task SaveHousehold_WhenUserIsInvitedMember_MarksStepSkippedWithoutReplacingMembers()
    {
        var repo = new RecordingOnboardingRepository { IsOwner = false };
        var handler = new SaveHouseholdStepHandler(repo);

        await handler.Handle(new SaveHouseholdStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            Members: [new RepresentedMemberInput("Pepe", "child")],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Empty(repo.MembersGuardados);
        Assert.Equal([(2, true)], repo.MarkedSteps);
    }
}
