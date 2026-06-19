using Nido.Application.Onboarding;
using Nido.Application.Onboarding.Exceptions;
using Nido.Application.Tests.Common.Security;

namespace Nido.Application.Tests.Onboarding;

public sealed class OnboardingEquipmentHandlerTests
{
    [Fact]
    public async Task SaveEquipment_WhenUserIsNotHouseholdMember_ThrowsAccessDenied()
    {
        var repo = new RecordingOnboardingRepository();
        var membershipService = new RecordingHouseholdMembershipService
        {
            MemberExceptionToThrow = new HouseholdAccessDeniedException()
        };
        var handler = new SaveEquipmentStepHandler(repo, new FakeHogarMembershipRepository(), membershipService);

        await Assert.ThrowsAsync<HouseholdAccessDeniedException>(() => handler.Handle(new SaveEquipmentStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: true,
            Equipments: [],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None));

        Assert.Empty(repo.EquipmentsGuardados);
        Assert.Empty(repo.MarkedSteps);
    }

    [Fact]
    public async Task SaveEquipment_WhenUserIsInvitedMember_MarksStepSkippedWithoutReplacingEquipment()
    {
        var repo = new RecordingOnboardingRepository();
        var handler = new SaveEquipmentStepHandler(
            repo,
            new FakeHogarMembershipRepository { IsOwner = false },
            new RecordingHouseholdMembershipService());

        await handler.Handle(new SaveEquipmentStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            Equipments: [new EquipmentInput(null, "Horno", "Oven", "new")],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Empty(repo.EquipmentsGuardados);
        Assert.Equal([(3, true)], repo.MarkedSteps);
    }
}
