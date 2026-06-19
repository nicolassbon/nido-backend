using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Hogares;

public sealed record EliminarHogarCommand(Guid UsuarioId, Guid HogarId, Guid ActiveHogarId);

public sealed class EliminarHogarHandler(IHogarRepository hogarRepo)
{
    public async Task Handle(EliminarHogarCommand command, CancellationToken ct)
    {
        var hogares = await hogarRepo.GetUserHogaresAsync(command.UsuarioId, ct);

        var hogar = hogares.FirstOrDefault(h => h.Id == command.HogarId)
            ?? throw new NotHouseholdMemberException();

        if (hogar.Rol != "owner")
            throw new NotHogarOwnerException();

        if (command.HogarId == command.ActiveHogarId)
            throw new CannotDeleteActiveHogarException();

        if (hogares.Count == 1)
            throw new UltimoHogarException();

        await hogarRepo.DeleteHogarAsync(command.HogarId, ct);
    }
}
