using Microsoft.EntityFrameworkCore;
using Nido.Application.UsuariosPerfil;
using Nido.Domain.Usuarios;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.UsuariosPerfil;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly NidoDbContext _dbContext;

    public UsuarioRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return null;

        return new Usuario(entity.Id, entity.Nombre, entity.Email, entity.Sexo, entity.FotoUrl);
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Usuarios.FirstOrDefaultAsync(x => x.Id == usuario.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario con ID {usuario.Id} no encontrado");

        entity.Nombre = usuario.Nombre;
        entity.Sexo = usuario.Sexo;
        entity.FotoUrl = usuario.FotoUrl;
        entity.UpdatedAt = DateTime.UtcNow;

        _dbContext.Usuarios.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
