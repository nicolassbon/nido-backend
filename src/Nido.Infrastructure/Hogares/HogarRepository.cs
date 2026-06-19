using Microsoft.EntityFrameworkCore;
using Nido.Application.Hogares;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Hogares;

public sealed class HogarRepository : IHogarRepository
{
    private readonly NidoDbContext _db;

    public HogarRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct)
    {
        return await _db.Hogares
            .AsNoTracking()
            .Where(h => h.Id == hogarId)
            .Select(h => new HogarInfo(h.Id, h.Nombre))
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct)
    {
        var hogar = await _db.Hogares.FindAsync([hogarId], ct);
        if (hogar is null) return;

        hogar.Nombre = nombre;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HogarInfo> CreateHogarAsync(Guid usuarioId, string nombre, CancellationToken ct)
    {
        var hogar = new Hogare
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            CreatedAt = DateTime.UtcNow
        };
        _db.Hogares.Add(hogar);
        _db.MiembrosHogars.Add(new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            HogarId = hogar.Id,
            Rol = "owner",
            Puntos = 0
        });
        await _db.SaveChangesAsync(ct);
        return new HogarInfo(hogar.Id, hogar.Nombre);
    }

    public Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid usuarioId, CancellationToken ct)
    {
        return _db.MiembrosHogars
            .Where(m => m.UsuarioId == usuarioId && m.NombreRepresentado == null)
            .Join(_db.Hogares,
                m => m.HogarId,
                h => h.Id,
                (m, h) => new HogarConRolInfo(h.Id, h.Nombre, m.Rol ?? "integrante"))
            .ToListAsync(ct);
    }

    public async Task DeleteHogarAsync(Guid hogarId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.Database.ExecuteSqlAsync(
                $"DELETE FROM asignaciones_tarea WHERE tarea_id IN (SELECT id FROM tareas WHERE hogar_id = {hogarId})", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM consumos_producto    WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM notas_receta         WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM resenias_receta      WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM stock_hogar          WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM lista_compras        WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM listas_compra_hogar  WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM gastos               WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM facturas             WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM electrodomesticos    WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM tareas               WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM presupuestos_mensuales WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM recetas_cocinadas    WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM recetas_guardadas_hogar WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM logros_hogar         WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM hogar_metas          WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM onboarding_goals     WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM onboarding_state     WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM invitaciones_hogar   WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM miembros_hogar       WHERE hogar_id = {hogarId}", ct);
            await _db.Database.ExecuteSqlAsync($"DELETE FROM hogares              WHERE id = {hogarId}", ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
