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
            Fecha = fecha,
            EsCompartido = command.EsCompartido,
            FacturaId = command.FacturaId,
        };

        if (command.EsCompartido && command.ParticipantesIds is { Count: > 0 } participantes)
        {
            foreach (var uid in participantes)
                gasto.Participantes.Add(new GastoParticipante { GastoId = gasto.Id, UsuarioId = uid });
        }

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
            .Include(g => g.Participantes)
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
            .Include(g => g.Participantes)
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

        var todosLosIds = miembros.Select(m => m.UsuarioId).ToHashSet();
        var gastosCompartidos = gastos.Where(g => g.EsCompartido).ToList();
        var gastosPersonales = gastos.Where(g => !g.EsCompartido).ToList();

        var totalPeriodo = gastosCompartidos.Sum(g => g.Monto);
        var totalPersonal = gastosPersonales.Sum(g => g.Monto);

        // Para cada miembro: cuánto aportó y cuánto le correspondía según los gastos compartidos en que participó
        var montoAportado = miembros.ToDictionary(m => m.UsuarioId, _ => 0m);
        var montoCorrespondido = miembros.ToDictionary(m => m.UsuarioId, _ => 0m);

        foreach (var gasto in gastosCompartidos)
        {
            // Quiénes participan: si no hay participantes explícitos → todos los miembros del hogar
            var participantesIds = gasto.Participantes.Count > 0
                ? gasto.Participantes.Select(p => p.UsuarioId).Where(id => todosLosIds.Contains(id)).ToList()
                : todosLosIds.ToList();

            if (participantesIds.Count == 0) continue;

            var cuota = Math.Round(gasto.Monto / participantesIds.Count, 2);

            foreach (var uid in participantesIds)
                if (montoCorrespondido.ContainsKey(uid))
                    montoCorrespondido[uid] += cuota;

            if (montoAportado.ContainsKey(gasto.PagadoPor))
                montoAportado[gasto.PagadoPor] += gasto.Monto;
        }

        var balances = miembros.Select(m => new BalanceMiembroResult(
            m.UsuarioId,
            m.Usuario.Nombre,
            _urlResolver.Resolve(m.Usuario.FotoStorageKey),
            montoAportado[m.UsuarioId],
            Math.Round(montoCorrespondido[m.UsuarioId], 2),
            Math.Round(montoAportado[m.UsuarioId] - montoCorrespondido[m.UsuarioId], 2)
        )).ToList();

        var deudas = CalcularDeudas(balances, miembros, _urlResolver);

        return new GetBalanceResult(balances, totalPeriodo, totalPersonal, deudas);
    }

    private static IReadOnlyList<DeudaResult> CalcularDeudas(
        List<BalanceMiembroResult> balances,
        List<MiembrosHogar> miembros,
        IPublicAssetUrlResolver urlResolver)
    {
        // Algoritmo greedy: acreedores (balance > 0) y deudores (balance < 0)
        var acreedores = balances
            .Where(b => b.Balance > 0)
            .Select(b => (Id: b.UsuarioId, Monto: b.Balance))
            .OrderByDescending(x => x.Monto)
            .ToList();

        var deudores = balances
            .Where(b => b.Balance < 0)
            .Select(b => (Id: b.UsuarioId, Monto: Math.Abs(b.Balance)))
            .OrderByDescending(x => x.Monto)
            .ToList();

        var infoMiembro = miembros.ToDictionary(
            m => m.UsuarioId,
            m => (m.Usuario.Nombre, FotoUrl: urlResolver.Resolve(m.Usuario.FotoStorageKey)));

        var deudas = new List<DeudaResult>();

        int i = 0, j = 0;
        while (i < acreedores.Count && j < deudores.Count)
        {
            var acreedor = acreedores[i];
            var deudor = deudores[j];
            var monto = Math.Min(acreedor.Monto, deudor.Monto);

            if (monto > 0.01m)
            {
                var infoAcreedor = infoMiembro.GetValueOrDefault(acreedor.Id);
                var infoDeudor = infoMiembro.GetValueOrDefault(deudor.Id);
                deudas.Add(new DeudaResult(
                    deudor.Id, infoDeudor.Nombre, infoDeudor.FotoUrl,
                    acreedor.Id, infoAcreedor.Nombre, infoAcreedor.FotoUrl,
                    Math.Round(monto, 2)));
            }

            acreedores[i] = (acreedor.Id, acreedor.Monto - monto);
            deudores[j] = (deudor.Id, deudor.Monto - monto);

            if (acreedores[i].Monto <= 0.01m) i++;
            if (deudores[j].Monto <= 0.01m) j++;
        }

        return deudas;
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

    public async Task<GastoResult?> UpdateGastoAsync(UpdateGastoCommand command, CancellationToken ct)
    {
        var gasto = await _db.Gastos
            .Include(g => g.PagadoPorNavigation)
            .Include(g => g.Participantes)
            .FirstOrDefaultAsync(g => g.Id == command.Id && g.HogarId == command.HogarId, ct);

        if (gasto is null) return null;

        gasto.Monto = command.Monto;
        gasto.Descripcion = command.Descripcion;
        gasto.Categoria = command.Categoria;
        gasto.Fecha = DateOnly.Parse(command.Fecha);
        gasto.PagadoPor = command.PagadoPorId;
        gasto.EsCompartido = command.EsCompartido;

        // Reemplazar participantes
        _db.GastoParticipantes.RemoveRange(gasto.Participantes);
        gasto.Participantes.Clear();

        if (command.EsCompartido && command.ParticipantesIds is { Count: > 0 } participantes)
        {
            foreach (var uid in participantes)
                gasto.Participantes.Add(new GastoParticipante { GastoId = gasto.Id, UsuarioId = uid });
        }

        await _db.SaveChangesAsync(ct);

        return ToGastoResult(gasto);
    }

    public async Task<DeleteGastoResult?> DeleteGastoAsync(Guid gastoId, Guid hogarId, CancellationToken ct)
    {
        var gasto = await _db.Gastos
            .FirstOrDefaultAsync(g => g.Id == gastoId && g.HogarId == hogarId, ct);

        if (gasto is null) return null;

        var facturaId = gasto.FacturaId;

        if (facturaId.HasValue)
        {
            var factura = await _db.Facturas.FindAsync([facturaId.Value], ct);
            if (factura is not null)
                factura.Pagada = false;
        }

        _db.Gastos.Remove(gasto);
        await _db.SaveChangesAsync(ct);

        return new DeleteGastoResult(facturaId.HasValue, facturaId);
    }

    public async Task<decimal?> GetPresupuestoAsync(Guid hogarId, int anio, int mes, CancellationToken ct)
    {
        var p = await _db.PresupuestosMensuales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.HogarId == hogarId && x.Anio == anio && x.Mes == mes, ct);
        return p?.Monto;
    }

    public async Task<decimal> UpsertPresupuestoAsync(Guid hogarId, int anio, int mes, decimal monto, CancellationToken ct)
    {
        var existing = await _db.PresupuestosMensuales
            .FirstOrDefaultAsync(x => x.HogarId == hogarId && x.Anio == anio && x.Mes == mes, ct);

        if (existing is null)
        {
            existing = new PresupuestoMensual
            {
                Id = Guid.NewGuid(),
                HogarId = hogarId,
                Anio = anio,
                Mes = mes,
                Monto = monto,
            };
            _db.PresupuestosMensuales.Add(existing);
        }
        else
        {
            existing.Monto = monto;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return existing.Monto;
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
        gasto.CreatedAt,
        gasto.FacturaId,
        gasto.EsCompartido,
        gasto.Participantes.Select(p => p.UsuarioId).ToList()
    );
}
