using Microsoft.EntityFrameworkCore;
using Nido.Application.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Auth;

public sealed class AuthRepository : IAuthRepository
{
    private readonly NidoDbContext _dbContext;

    public AuthRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => _dbContext.Usuarios.AnyAsync(x => x.Email == email, cancellationToken);

    public async Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(
        string nombre,
        string email,
        string passwordHash,
        string sexo,
        string? fotoUrl,
        CancellationToken cancellationToken)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Email = email,
            PasswordHash = passwordHash,
            Sexo = sexo,
            FotoUrl = fotoUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hogar = new Hogare
        {
            Id = Guid.NewGuid(),
            Nombre = $"Hogar de {nombre}",
            CreatedAt = DateTime.UtcNow
        };

        var membership = new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            HogarId = hogar.Id,
            Rol = "owner",
            Puntos = 0
        };

        var state = new OnboardingState
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            HogarId = hogar.Id,
            Step1CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Usuarios.Add(usuario);
        _dbContext.Hogares.Add(hogar);
        _dbContext.MiembrosHogars.Add(membership);
        _dbContext.OnboardingStates.Add(state);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (usuario.Id, hogar.Id);
    }
}
