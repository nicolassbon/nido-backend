using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.Assets;
using Nido.Application.Planificador;
using Nido.Application.Tareas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Planificador;

public sealed class PlanificadorRepository : IPlanificadorRepository
{
    private readonly NidoDbContext _db;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public PlanificadorRepository(NidoDbContext db, IPublicAssetUrlResolver assetUrlResolver)
    {
        _db = db;
        _assetUrlResolver = assetUrlResolver;
    }

    public async Task<PlanificadorSemanaResult> GetOrCreateSemanaAsync(
        Guid hogarId, DateOnly fechaInicio, CancellationToken ct)
    {
        var semana = await _db.PlanificadorSemanas
            .Include(s => s.Items)
                .ThenInclude(i => i.Receta)
            .Include(s => s.Items)
                .ThenInclude(i => i.Tarea)
                    .ThenInclude(t => t!.AsignacionesTareas)
                        .ThenInclude(a => a.Usuario)
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
        TareaResult? tarea = null;
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
        else if (command.TipoComida == "tarea")
        {
            var titulo = command.TituloLibre?.Trim();
            if (string.IsNullOrWhiteSpace(titulo))
            {
                throw new InvalidOperationException("El titulo de la tarea es obligatorio.");
            }

            tarea = await CreateTaskAsync(command.HogarId, command.UsuarioId, titulo, command.Fecha, command.Hora, command.AsignadoA, ct);
        }

        var maxOrden = await _db.PlanificadorItems
            .Where(i => i.SemanaId == semana.Id && i.Fecha == command.Fecha && i.TipoComida == command.TipoComida)
            .Select(i => (int?)i.Orden)
            .MaxAsync(ct) ?? -1;

        var item = new PlanificadorItem
        {
            Id          = Guid.NewGuid(),
            SemanaId    = semana.Id,
            TareaId     = tarea?.Id,
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
            item.TareaId, item.RecetaId, recetaNombre, ResolveImageUrl(imagenUrl),
            item.TituloLibre, item.Hora, tarea?.Estado, ToAssignment(tarea?.AsignadoA), item.Orden, item.CreadoPor);
    }

    public async Task<bool> DeleteItemAsync(DeletePlanificadorItemCommand command, CancellationToken ct)
    {
        var item = await _db.PlanificadorItems
            .Include(i => i.Semana)
            .Include(i => i.Tarea)
                .ThenInclude(t => t!.AsignacionesTareas)
            .FirstOrDefaultAsync(i => i.Id == command.ItemId && i.Semana.HogarId == command.HogarId, ct);

        if (item is null) return false;

        if (item.TareaId.HasValue && item.Tarea is not null)
        {
            _db.AsignacionesTareas.RemoveRange(item.Tarea.AsignacionesTareas);

            var notifs = await _db.Notificaciones
                .Where(n => n.ReferenciaTipo == "tarea" && n.ReferenciaId == item.TareaId.Value)
                .ToListAsync(ct);
            _db.Notificaciones.RemoveRange(notifs);
            _db.Tareas.Remove(item.Tarea);
        }

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
            .Include(i => i.Tarea)
                .ThenInclude(t => t!.AsignacionesTareas)
                    .ThenInclude(a => a.Usuario)
            .FirstOrDefaultAsync(i => i.Id == command.ItemId && i.Semana.HogarId == command.HogarId, ct);

        if (item is null) return null;

        string? recetaNombre = null;
        string? imagenUrl = null;
        TareaResult? tareaResult = null;
        var isTask = item.TipoComida == "tarea";

        if (isTask)
        {
            var titulo = command.TituloLibre?.Trim();
            if (string.IsNullOrWhiteSpace(titulo)) return null;

            item.RecetaId = null;
            item.Receta = null;
            item.TituloLibre = titulo;
            item.ImagenUrl = null;
            tareaResult = await UpsertTaskAsync(item, command.UsuarioId, titulo, command.AsignadoA, ct);
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

        if (isTask && item.Tarea is not null)
        {
            item.Tarea.FechaLimite = BuildDueDate(item.Fecha, item.Hora);
        }

        await _db.SaveChangesAsync(ct);

        return new PlanificadorItemResult(
            item.Id, item.Fecha, item.TipoComida,
            item.TareaId, item.RecetaId, recetaNombre ?? item.Receta?.Nombre,
            ResolveImageUrl(imagenUrl ?? item.ImagenUrl ?? item.Receta?.ImagenUrl),
            item.TituloLibre, item.Hora, tareaResult?.Estado ?? item.Tarea?.Estado, ToAssignment(tareaResult?.AsignadoA) ?? ToAssignment(item.Tarea), item.Orden, item.CreadoPor);
    }

    private static DateOnly GetLunes(DateOnly fecha)
    {
        var dow = (int)fecha.DayOfWeek;
        var offset = dow == 0 ? 6 : dow - 1; // Sunday=0 in .NET
        return fecha.AddDays(-offset);
    }

    private PlanificadorSemanaResult ToResult(PlanificadorSemana semana)
        => new(
            semana.Id,
            semana.FechaInicio,
            semana.Items
                .OrderBy(i => i.Fecha)
                .ThenBy(i => i.Orden)
                .Select(i => new PlanificadorItemResult(
                    i.Id, i.Fecha, i.TipoComida,
                    i.TareaId, i.RecetaId, i.Receta?.Nombre,
                    ResolveImageUrl(i.ImagenUrl ?? i.Receta?.ImagenUrl),
                    i.TituloLibre, i.Hora, i.Tarea?.Estado, ToAssignment(i.Tarea), i.Orden, i.CreadoPor))
                .ToList());

    private string? ResolveImageUrl(string? imageUrl)
        => _assetUrlResolver.Resolve(imageUrl) ?? imageUrl;

    private async Task<TareaResult> CreateTaskAsync(
        Guid hogarId,
        Guid creadoPor,
        string titulo,
        DateOnly fecha,
        string? hora,
        Guid? asignadoA,
        CancellationToken ct)
    {
        var tarea = new Tarea
        {
            HogarId = hogarId,
            CreadoPor = creadoPor,
            Titulo = titulo,
            Descripcion = null,
            Estado = "pendiente",
            FechaLimite = BuildDueDate(fecha, hora),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Tareas.Add(tarea);
        await _db.SaveChangesAsync(ct);

        AsignacionResult? asignacion = null;
        if (asignadoA.HasValue)
        {
            asignacion = await AssignTaskAsync(tarea, asignadoA.Value, creadoPor, titulo, ct);
        }

        return new TareaResult(
            tarea.Id,
            tarea.HogarId,
            tarea.Titulo,
            tarea.Descripcion,
            tarea.Estado ?? "pendiente",
            tarea.FechaLimite,
            tarea.FechaCompletado,
            tarea.CreadoPor,
            await GetUserNameAsync(creadoPor, ct),
            tarea.CompletadoPor,
            null,
            asignacion,
            tarea.CreatedAt);
    }

    private async Task<TareaResult> UpsertTaskAsync(
        PlanificadorItem item,
        Guid usuarioId,
        string titulo,
        Guid? asignadoA,
        CancellationToken ct)
    {
        if (item.Tarea is null)
        {
            var created = await CreateTaskAsync(item.Semana.HogarId, usuarioId, titulo, item.Fecha, item.Hora, asignadoA, ct);
            item.TareaId = created.Id;
            item.Tarea = await _db.Tareas
                .Include(t => t.AsignacionesTareas)
                    .ThenInclude(a => a.Usuario)
                .Include(t => t.CreadoPorNavigation)
                .FirstAsync(t => t.Id == created.Id, ct);
            return created;
        }

        item.Tarea.Titulo = titulo;
        item.Tarea.FechaLimite = BuildDueDate(item.Fecha, item.Hora);
        var asignacion = await ReplaceAssignmentAsync(item.Tarea, asignadoA, usuarioId, ct);

        return new TareaResult(
            item.Tarea.Id,
            item.Tarea.HogarId,
            item.Tarea.Titulo,
            item.Tarea.Descripcion,
            item.Tarea.Estado ?? "pendiente",
            item.Tarea.FechaLimite,
            item.Tarea.FechaCompletado,
            item.Tarea.CreadoPor,
            item.Tarea.CreadoPorNavigation?.Nombre ?? await GetUserNameAsync(item.Tarea.CreadoPor, ct),
            item.Tarea.CompletadoPor,
            item.Tarea.CompletadoPorNavigation?.Nombre,
            asignacion,
            item.Tarea.CreatedAt);
    }

    private async Task<AsignacionResult?> ReplaceAssignmentAsync(
        Tarea tarea,
        Guid? asignadoA,
        Guid asignadoPor,
        CancellationToken ct)
    {
        var actual = tarea.AsignacionesTareas
            .OrderByDescending(a => a.FechaAsignacion)
            .FirstOrDefault();

        if (actual is not null && asignadoA == actual.UsuarioId)
        {
            return new AsignacionResult(actual.UsuarioId, actual.Usuario.Nombre, actual.Usuario.FotoStorageKey);
        }

        _db.AsignacionesTareas.RemoveRange(tarea.AsignacionesTareas);
        tarea.AsignacionesTareas.Clear();

        if (!asignadoA.HasValue)
        {
            return null;
        }

        return await AssignTaskAsync(tarea, asignadoA.Value, asignadoPor, tarea.Titulo, ct);
    }

    private async Task<AsignacionResult> AssignTaskAsync(
        Tarea tarea,
        Guid usuarioId,
        Guid asignadoPor,
        string titulo,
        CancellationToken ct)
    {
        var asignacionEntity = new AsignacionesTarea
        {
            TareaId = tarea.Id,
            Tarea = tarea,
            UsuarioId = usuarioId,
            FechaAsignacion = DateTime.UtcNow,
        };

        _db.AsignacionesTareas.Add(asignacionEntity);
        tarea.AsignacionesTareas.Add(asignacionEntity);

        var usuario = await _db.Usuarios.FirstAsync(u => u.Id == usuarioId, ct);
        asignacionEntity.Usuario = usuario;

        if (usuarioId != asignadoPor)
        {
            var asignadorNombre = await GetUserNameAsync(asignadoPor, ct);
            _db.Notificaciones.Add(new Notificacione
            {
                UsuarioId = usuarioId,
                Tipo = "asignacion_tarea",
                ReferenciaTipo = "tarea",
                ReferenciaId = tarea.Id,
                Mensaje = $"{asignadorNombre} te asignó la tarea \"{titulo}\"",
                Leida = false,
                CreatedAt = DateTime.UtcNow,
            });
        }

        return new AsignacionResult(usuario.Id, usuario.Nombre, usuario.FotoStorageKey);
    }

    private async Task<string> GetUserNameAsync(Guid usuarioId, CancellationToken ct)
    {
        return await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Nombre)
            .FirstOrDefaultAsync(ct) ?? "Alguien";
    }

    private static DateTime BuildDueDate(DateOnly fecha, string? hora)
    {
        if (TimeOnly.TryParse(hora, out var parsedTime))
        {
            return fecha.ToDateTime(parsedTime);
        }

        return fecha.ToDateTime(new TimeOnly(23, 59));
    }

    private static PlanificadorAsignacionResult? ToAssignment(Tarea? tarea)
    {
        var asignacion = tarea?.AsignacionesTareas
            .OrderByDescending(a => a.FechaAsignacion)
            .FirstOrDefault();

        return asignacion is null
            ? null
            : new PlanificadorAsignacionResult(asignacion.UsuarioId, asignacion.Usuario.Nombre, asignacion.Usuario.FotoStorageKey);
    }

    private static PlanificadorAsignacionResult? ToAssignment(AsignacionResult? asignacion)
        => asignacion is null ? null : new PlanificadorAsignacionResult(asignacion.UsuarioId, asignacion.Nombre, asignacion.FotoStorageKey);
}
