namespace Nido.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Usuario(Guid id, string nombre, string email, string sexo, string? telefono = null, string? fotoStorageKey = null, string? fotoUrl = null, DateTime createdAt = default)
    {
        Id = id;
        Nombre = nombre;
        Email = email;
        Sexo = sexo;
        Telefono = telefono;
        FotoStorageKey = fotoStorageKey;
        FotoUrl = fotoUrl;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Sexo { get; set; } = string.Empty;

    public string? Telefono { get; set; } = string.Empty;

    public string? FotoStorageKey { get; set; }

    public string? FotoUrl { get; set; }

    public DateTime CreatedAt { get; private set; }


    public void ActualizarPerfil(string nombre, string sexo, string? telefono = null, string? fotoStorageKey = null)
    {
        Nombre = nombre;
        Sexo = sexo;
        Telefono = telefono;
        FotoStorageKey = fotoStorageKey;
    }
}

