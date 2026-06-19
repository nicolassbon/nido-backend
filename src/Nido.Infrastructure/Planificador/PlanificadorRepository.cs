using Microsoft.EntityFrameworkCore;
using Nido.Application.Planificador;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Planificador;

public sealed class PlanificadorRepository : IPlanificadorRepository
{
    private readonly NidoDbContext _db;

    public PlanificadorRepository(NidoDbContext db) => _db = db;

    public async Task<PlanificadorSemanaResult> GetOrCreateSemanaAsync(
        Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
    {
        var semana = await _db.PlanificadorSemanas
            .Include(s => s.Items)
                .ThenInclude(i => i.Receta)
            .FirstOrDefaultAsync(s => s.HogarId == hogarId && s.FechaInicio == fechaInicio, ct);

        if (semana is null)
        {
            semana = new PlanificadorSemana
            {
                Id          = Guid.NewGuid(),
                HogarId     = hogarId,
                FechaInicio = fechaInicio,
                CreatedAt   = DateTime.UtcNow,
            };
            _db.PlanificadorSemanas.Add(semana);
            await _db.SaveChangesAsync(ct);
        }

        return ToResult(semana);
    }

    public async Task<PlanificadorItemResult> AddItemAsync(
        AddPlanificadorItemCommand command, CancellationToken ct)
    {
        // Asegurar que exista la semana
        var lunes = GetLunes(command.Fecha);
        var semana = await _db.PlanificadorSemanas
            .FirstOrDefaultAsync(s => s.HogarId == command.HogarId && s.FechaInicio == lunes, ct);

        if (semana is null)
        {
            semana = new PlanificadorSemana
            {
                Id          = Guid.NewGuid(),
                HogarId     = command.HogarId,
                FechaInicio = lunes,
                CreatedAt   = DateTime.UtcNow,
            };
            _db.PlanificadorSemanas.Add(semana);
            await _db.SaveChangesAsync(ct);
        }

        string? imagenUrl = null;
        string? recetaNombre = null;
        if (command.RecetaId.HasValue)
        {
            var receta = await _db.Recetas
                .AsNoTracking()
                .Where(r => r.Id == command.RecetaId.Value)
                .Select(r => new { r.Nombre, r.ImagenUrl })
                .FirstOrDefaultAsync(ct);

            recetaNombre = receta?.Nombre;
            imagenUrl    = receta?.ImagenUrl;
        }

        var maxOrden = await _db.PlanificadorItems
            .Where(i => i.SemanaId == semana.Id && i.Fecha == command.Fecha && i.TipoComida == command.TipoComida)
            .Select(i => (int?)i.Orden)
            .MaxAsync(ct) ?? -1;

        var item = new PlanificadorItem
        {
            Id          = Guid.NewGuid(),
            SemanaId    = semana.Id,
            Fecha       = command.Fecha,
            TipoComida  = command.TipoComida,
            RecetaId    = command.RecetaId,
            TituloLibre = command.TituloLibre,
            ImagenUrl   = imagenUrl,
            Hora        = command.Hora,
            Orden       = maxOrden + 1,
            CreadoPor   = command.UsuarioId,
            CreatedAt   = DateTime.UtcNow,
        };

        _db.PlanificadorItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return new PlanificadorItemResult(
            item.Id, item.Fecha, item.TipoComida,
            item.RecetaId, recetaNombre, imagenUrl,
            item.TituloLibre, item.Hora, item.Orden, item.CreadoPor);
    }

    public async Task<bool> DeleteItemAsync(DeletePlanificadorItemCommand command, CancellationToken ct)
    {
        var item = await _db.PlanificadorItems
            .Include(i => i.Semana)
            .FirstOrDefaultAsync(i => i.Id == command.ItemId && i.Semana.HogarId == command.HogarId, ct);

        if (item is null) return false;

        _db.PlanificadorItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PlanificadorItemResult?> UpdateItemAsync(
        UpdatePlanificadorItemCommand command, CancellationToken ct)
    {
        var item = await _db.PlanificadorItems
            .Include(i => i.Semana)
            .Include(i => i.Receta)
            .FirstOrDefaultAsync(i => i.Id == command.ItemId && i.Semana.HogarId == command.HogarId, ct);

        if (item is null) return null;

        string? recetaNombre = null;
        string? imagenUrl = null;
        var isTask = item.TipoComida == "tarea";

        if (isTask)
        {
            var titulo = command.TituloLibre?.Trim();
            if (string.IsNullOrWhiteSpace(titulo)) return null;

            item.RecetaId = null;
            item.Receta = null;
            item.TituloLibre = titulo;
            item.ImagenUrl = null;
        }
        else
        {
            if (!command.RecetaId.HasValue) return null;

            var receta = await _db.Recetas
                .AsNoTracking()
                .Where(r => r.Id == command.RecetaId.Value)
                .Select(r => new { r.Id, r.Nombre, r.ImagenUrl })
                .FirstOrDefaultAsync(ct);

            if (receta is null) return null;

            item.RecetaId = receta.Id;
            item.Receta = null;
            item.TituloLibre = null;
            item.ImagenUrl = receta.ImagenUrl;
            recetaNombre = receta.Nombre;
            imagenUrl = receta.ImagenUrl;
        }

        item.Hora = string.IsNullOrWhiteSpace(command.Hora) ? null : command.Hora;
        await _db.SaveChangesAsync(ct);

        return new PlanificadorItemResult(
            item.Id, item.Fecha, item.TipoComida,
            item.RecetaId, recetaNombre ?? item.Receta?.Nombre,
            imagenUrl ?? item.ImagenUrl ?? item.Receta?.ImagenUrl,
            item.TituloLibre, item.Hora, item.Orden, item.CreadoPor);
    }

    private static DateOnly GetLunes(DateOnly fecha)
    {
        var dow = (int)fecha.DayOfWeek;
        var offset = dow == 0 ? 6 : dow - 1; // Sunday=0 in .NET
        return fecha.AddDays(-offset);
    }

    private static PlanificadorSemanaResult ToResult(PlanificadorSemana semana)
        => new(
            semana.Id,
            semana.FechaInicio,
            semana.Items
                .OrderBy(i => i.Fecha)
                .ThenBy(i => i.Orden)
                .Select(i => new PlanificadorItemResult(
                    i.Id, i.Fecha, i.TipoComida,
                    i.RecetaId, i.Receta?.Nombre,
                    i.ImagenUrl ?? i.Receta?.ImagenUrl,
                    i.TituloLibre, i.Hora, i.Orden, i.CreadoPor))
                .ToList());
}
