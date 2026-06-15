using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.Assets;
using Nido.Application.Finanzas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Finanzas;

public sealed class FinanzasRepository : IFinanzasRepository
{
    private readonly NidoDbContext _db;
    private readonly IPublicAssetUrlResolver _urlResolver;

    public FinanzasRepository(NidoDbContext db, IPublicAssetUrlResolver urlResolver)
    {
        _db = db;
        _urlResolver = urlResolver;
    }

    public async Task<GastoResult> CreateGastoAsync(CreateGastoCommand command, CancellationToken ct)
    {
        var fecha = DateOnly.Parse(command.Fecha);

        var gasto = new Gasto
        {
            Id = Guid.NewGuid(),
            HogarId = command.HogarId,
            PagadoPor = command.PagadoPorId,
            Monto = command.Monto,
            Descripcion = command.Descripcion,
            Categoria = command.Categoria,
            Fecha = fecha
        };

        _db.Gastos.Add(gasto);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(gasto).Reference(g => g.PagadoPorNavigation).LoadAsync(ct);

        return ToGastoResult(gasto);
    }

    public async Task<GetGastosResult> GetGastosAsync(GetGastosQuery query, CancellationToken ct)
    {
        var q = _db.Gastos
            .AsNoTracking()
            .Include(g => g.PagadoPorNavigation)
            .Where(g => g.HogarId == query.HogarId);

        if (!string.IsNullOrWhiteSpace(query.Desde) && DateOnly.TryParse(query.Desde, out var desde))
            q = q.Where(g => g.Fecha >= desde);

        if (!string.IsNullOrWhiteSpace(query.Hasta) && DateOnly.TryParse(query.Hasta, out var hasta))
            q = q.Where(g => g.Fecha <= hasta);

        if (!string.IsNullOrWhiteSpace(query.Categoria))
            q = q.Where(g => g.Categoria == query.Categoria);

        var gastos = await q
            .OrderByDescending(g => g.Fecha)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        return new GetGastosResult(gastos.Select(ToGastoResult).ToList(), gastos.Sum(g => g.Monto));
    }

    public async Task<bool?> GetModoAhorroAsync(Guid hogarId, CancellationToken ct)
    {
        var hogar = await _db.Hogares.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hogarId, ct);
        return hogar?.ModoAhorro;
    }

    public async Task<bool> ToggleModoAhorroAsync(Guid hogarId, bool activo, CancellationToken ct)
    {
        var hogar = await _db.Hogares.FindAsync([hogarId], ct);
        if (hogar is null) return false;

        hogar.ModoAhorro = activo;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<GetBalanceResult> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct)
    {
        var q = _db.Gastos
            .AsNoTracking()
            .Where(g => g.HogarId == query.HogarId);

        if (!string.IsNullOrWhiteSpace(query.Desde) && DateOnly.TryParse(query.Desde, out var desde))
            q = q.Where(g => g.Fecha >= desde);

        if (!string.IsNullOrWhiteSpace(query.Hasta) && DateOnly.TryParse(query.Hasta, out var hasta))
            q = q.Where(g => g.Fecha <= hasta);

        var gastos = await q.ToListAsync(ct);

        var miembros = await _db.MiembrosHogars
            .AsNoTracking()
            .Include(m => m.Usuario)
            .Where(m => m.HogarId == query.HogarId)
            .ToListAsync(ct);

        var totalPeriodo = gastos.Sum(g => g.Monto);
        var cantidadMiembros = miembros.Count;
        var montoCorrespondido = cantidadMiembros > 0
            ? Math.Round(totalPeriodo / cantidadMiembros, 2)
            : 0m;

        var aportadoPorUsuario = gastos
            .GroupBy(g => g.PagadoPor)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

        var balances = miembros.Select(m =>
        {
            var aportado = aportadoPorUsuario.GetValueOrDefault(m.UsuarioId, 0m);
            return new BalanceMiembroResult(
                m.UsuarioId,
                m.Usuario.Nombre,
                _urlResolver.Resolve(m.Usuario.FotoStorageKey),
                aportado,
                montoCorrespondido,
                Math.Round(aportado - montoCorrespondido, 2)
            );
        }).ToList();

        return new GetBalanceResult(balances, totalPeriodo);
    }

    public async Task<FacturaResult> CreateFacturaAsync(
        CreateFacturaCommand command, Guid facturaId, string? storageKey, CancellationToken ct)
    {
        DateOnly? fechaVencimiento = null;
        if (!string.IsNullOrWhiteSpace(command.FechaVencimiento))
            fechaVencimiento = DateOnly.Parse(command.FechaVencimiento);

        var factura = new Factura
        {
            Id = facturaId,
            HogarId = command.HogarId,
            CreadoPor = command.CreadoPor,
            Nombre = command.Nombre,
            Tipo = command.Tipo,
            Monto = command.Monto,
            FechaVencimiento = fechaVencimiento,
            ArchivoStorageKey = storageKey,
            Pagada = false
        };

        _db.Facturas.Add(factura);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(factura).Reference(f => f.CreadoPorNavigation).LoadAsync(ct);

        return ToFacturaResult(factura);
    }

    public async Task<IReadOnlyList<FacturaResult>> GetFacturasAsync(GetFacturasQuery query, CancellationToken ct)
    {
        var q = _db.Facturas
            .AsNoTracking()
            .Include(f => f.CreadoPorNavigation)
            .Where(f => f.HogarId == query.HogarId);

        if (!string.IsNullOrWhiteSpace(query.Tipo))
            q = q.Where(f => f.Tipo == query.Tipo);

        if (query.Pagada.HasValue)
            q = q.Where(f => f.Pagada == query.Pagada.Value);

        if (query.ProximaDias.HasValue)
        {
            var limite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(query.ProximaDias.Value));
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(f => f.FechaVencimiento.HasValue
                && f.FechaVencimiento.Value >= hoy
                && f.FechaVencimiento.Value <= limite);
        }

        var facturas = await q
            .OrderBy(f => f.FechaVencimiento)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return facturas.Select(ToFacturaResult).ToList();
    }

    public async Task<FacturaResult?> MarcarPagadaAsync(Guid facturaId, Guid hogarId, CancellationToken ct)
    {
        var factura = await _db.Facturas
            .Include(f => f.CreadoPorNavigation)
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.HogarId == hogarId, ct);

        if (factura is null) return null;

        factura.Pagada = true;
        await _db.SaveChangesAsync(ct);

        return ToFacturaResult(factura);
    }

    public async Task<string?> DeleteFacturaAsync(Guid facturaId, Guid hogarId, CancellationToken ct)
    {
        var factura = await _db.Facturas
            .FirstOrDefaultAsync(f => f.Id == facturaId && f.HogarId == hogarId, ct);

        if (factura is null) return null;

        var storageKey = factura.ArchivoStorageKey;
        _db.Facturas.Remove(factura);
        await _db.SaveChangesAsync(ct);

        // Return empty string to signal "found but no file" vs null for "not found"
        return storageKey ?? string.Empty;
    }

    private FacturaResult ToFacturaResult(Factura factura)
    {
        int? diasParaVencer = factura.FechaVencimiento.HasValue
            ? (factura.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days
            : null;

        return new FacturaResult(
            factura.Id,
            factura.Nombre,
            factura.Tipo,
            factura.Monto,
            factura.FechaVencimiento?.ToString("yyyy-MM-dd"),
            _urlResolver.Resolve(factura.ArchivoStorageKey),
            factura.Pagada,
            diasParaVencer,
            factura.CreadoPor,
            factura.CreadoPorNavigation?.Nombre ?? string.Empty,
            factura.CreatedAt
        );
    }

    private static GastoResult ToGastoResult(Gasto gasto) => new(
        gasto.Id,
        gasto.Monto,
        gasto.Descripcion,
        gasto.Categoria,
        gasto.Fecha.ToString("yyyy-MM-dd"),
        gasto.PagadoPor,
        gasto.PagadoPorNavigation?.Nombre ?? string.Empty,
        gasto.CreatedAt
    );
}
