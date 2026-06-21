using Nido.Application.Hogares;
using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Tests.Hogares;

public sealed class EliminarHogarHandlerTests
{
    private static readonly Guid UsuarioId   = Guid.NewGuid();
    private static readonly Guid HogarOwned  = Guid.NewGuid();
    private static readonly Guid HogarActivo = Guid.NewGuid();

    [Fact]
    public async Task Handle_OwnerEliminaHogarNoActivo_LlamaDelete()
    {
        var repo = new FakeHogarRepository(UsuarioId,
        [
            new HogarConRolInfo(HogarOwned,  "Casa verano", "owner"),
            new HogarConRolInfo(HogarActivo, "Casa principal", "owner"),
        ]);
        var handler = new EliminarHogarHandler(repo);
        var command = new EliminarHogarCommand(UsuarioId, HogarOwned, HogarActivo);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(HogarOwned, repo.DeletedHogarId);
    }

    [Fact]
    public async Task Handle_HogarNoPertenecealUsuario_LanzaNotHouseholdMember()
    {
        var repo = new FakeHogarRepository(UsuarioId,
        [
            new HogarConRolInfo(HogarActivo, "Casa principal", "owner"),
        ]);
        var handler = new EliminarHogarHandler(repo);
        var ajeno = Guid.NewGuid();
        var command = new EliminarHogarCommand(UsuarioId, ajeno, HogarActivo);

        await Assert.ThrowsAsync<NotHouseholdMemberException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioNoEsOwner_LanzaNotHogarOwner()
    {
        var repo = new FakeHogarRepository(UsuarioId,
        [
            new HogarConRolInfo(HogarOwned,  "Casa verano",   "integrante"),
            new HogarConRolInfo(HogarActivo, "Casa principal", "owner"),
        ]);
        var handler = new EliminarHogarHandler(repo);
        var command = new EliminarHogarCommand(UsuarioId, HogarOwned, HogarActivo);

        await Assert.ThrowsAsync<NotHogarOwnerException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IntentaEliminarHogarActivo_LanzaCannotDeleteActiveHogar()
    {
        var repo = new FakeHogarRepository(UsuarioId,
        [
            new HogarConRolInfo(HogarOwned,  "Casa verano",   "owner"),
            new HogarConRolInfo(HogarActivo, "Casa principal", "owner"),
        ]);
        var handler = new EliminarHogarHandler(repo);
        var command = new EliminarHogarCommand(UsuarioId, HogarActivo, HogarActivo);

        await Assert.ThrowsAsync<CannotDeleteActiveHogarException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EsElUnicoHogar_LanzaUltimoHogar()
    {
        var repo = new FakeHogarRepository(UsuarioId,
        [
            new HogarConRolInfo(HogarOwned, "Casa única", "owner"),
        ]);
        var handler = new EliminarHogarHandler(repo);
        var command = new EliminarHogarCommand(UsuarioId, HogarOwned, Guid.NewGuid());

        await Assert.ThrowsAsync<UltimoHogarException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    private sealed class FakeHogarRepository(Guid usuarioId, List<HogarConRolInfo> hogares) : IHogarRepository
    {
        public Guid? DeletedHogarId { get; private set; }

        public Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid uid, CancellationToken ct)
            => Task.FromResult(uid == usuarioId ? hogares : []);

        public Task DeleteHogarAsync(Guid hogarId, CancellationToken ct)
        {
            DeletedHogarId = hogarId;
            return Task.CompletedTask;
        }

        public Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct) => Task.FromResult<HogarInfo?>(null);
        public Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct) => Task.CompletedTask;
        public Task<HogarInfo> CreateHogarAsync(Guid uid, string nombre, CancellationToken ct)
            => Task.FromResult(new HogarInfo(Guid.NewGuid(), nombre));
    }
}
