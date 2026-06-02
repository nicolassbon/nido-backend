using Microsoft.EntityFrameworkCore;
using Nido.Application.UsuariosPerfil;
using Nido.Domain.Usuarios;
using Nido.Infrastructure.Persistence;
using Nido.Application.Common.ProfileImages;

namespace Nido.Infrastructure.UsuariosPerfil;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly NidoDbContext _dbContext;
    private readonly IProfileImagePublicUrlResolver _profileImagePublicUrlResolver;

    public UsuarioRepository(NidoDbContext dbContext, IProfileImagePublicUrlResolver profileImagePublicUrlResolver)
    {
        _dbContext = dbContext;
        _profileImagePublicUrlResolver = profileImagePublicUrlResolver;
    }

    public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return null;

        return new Usuario(entity.Id, entity.Nombre, entity.Email, entity.Sexo, entity.Telefono, entity.FotoStorageKey, entity.FotoUrl, entity.CreatedAt);
    }

    public async Task<IReadOnlyList<string>> GetRestriccionesUsuarioAsync(Guid usuarioId, string tipo, CancellationToken cancellationToken)
    {
        return await _dbContext.RestriccionesUsuarios
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Restriccion.Tipo == tipo)
            .Select(x => x.Restriccion.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Usuarios.FirstOrDefaultAsync(x => x.Id == usuario.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario con ID {usuario.Id} no encontrado");

        entity.Nombre = usuario.Nombre;
        entity.Sexo = usuario.Sexo;
        entity.Telefono = usuario.Telefono;
        entity.FotoStorageKey = usuario.FotoStorageKey;
        entity.FotoUrl = usuario.FotoUrl;
        entity.UpdatedAt = DateTime.UtcNow;

        _dbContext.Usuarios.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceRestriccionesUsuarioAsync(Guid usuarioId, string tipo, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RestriccionesUsuarios
            .Where(x => x.UsuarioId == usuarioId && x.Restriccion.Tipo == tipo)
            .ToListAsync(cancellationToken);

        _dbContext.RestriccionesUsuarios.RemoveRange(existing);

        foreach (var id in restriccionIds)
        {
            _dbContext.RestriccionesUsuarios.Add(new Nido.Infrastructure.Persistence.Entities.RestriccionesUsuario
            {
                UsuarioId = usuarioId,
                RestriccionId = id
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
