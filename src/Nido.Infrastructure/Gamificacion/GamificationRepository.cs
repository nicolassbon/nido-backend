using System.Data;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Gamificacion;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Gamificacion;

public sealed class GamificationRepository : IGamificationRepository
{
    private readonly NidoDbContext _db;

    public GamificationRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<int> CountCurrentlyCompletedTasksAsync(Guid usuarioId, CancellationToken ct)
    {
        return await _db.Tareas
            .Where(t => t.Estado == "completada"
                        && t.CompletadoPor == usuarioId
                        && t.Hogar.MiembrosHogars.Any(m =>
                            m.UsuarioId == usuarioId && m.NombreRepresentado == null))
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<int>> GetUnlockedLevelsAsync(Guid usuarioId, CancellationToken ct)
    {
        return await _db.GamificacionNivelesDesbloqueados
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => x.Nivel)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> InsertMissingUnlocksAsync(
        Guid usuarioId, IEnumerable<int> levels, DateTime unlockedAt, CancellationToken ct)
    {
        var levelList = levels.Distinct().ToList();
        if (levelList.Count == 0)
            return Array.Empty<int>();

        var conn = _db.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        var valueRows = new List<string>(levelList.Count);

        for (var i = 0; i < levelList.Count; i++)
        {
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = $"id{i}";
            idParameter.Value = Guid.NewGuid();
            cmd.Parameters.Add(idParameter);

            var usuarioParameter = cmd.CreateParameter();
            usuarioParameter.ParameterName = $"usuarioId{i}";
            usuarioParameter.Value = usuarioId;
            cmd.Parameters.Add(usuarioParameter);

            var levelParameter = cmd.CreateParameter();
            levelParameter.ParameterName = $"nivel{i}";
            levelParameter.Value = levelList[i];
            cmd.Parameters.Add(levelParameter);

            var unlockedAtParameter = cmd.CreateParameter();
            unlockedAtParameter.ParameterName = $"desbloqueadoEn{i}";
            unlockedAtParameter.Value = unlockedAt;
            cmd.Parameters.Add(unlockedAtParameter);

            valueRows.Add($"(@id{i}, @usuarioId{i}, @nivel{i}, @desbloqueadoEn{i})");
        }

        cmd.CommandText = $@"
            INSERT INTO gamificacion_niveles_desbloqueados (id, usuario_id, nivel, desbloqueado_en)
            VALUES {string.Join(", ", valueRows)}
            ON CONFLICT (usuario_id, nivel) DO NOTHING
            RETURNING nivel";

        if (conn.State == ConnectionState.Closed)
        {
            await conn.OpenAsync(ct);
        }

        var inserted = new List<int>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            inserted.Add(reader.GetInt32(0));
        }

        return inserted;
    }
}
