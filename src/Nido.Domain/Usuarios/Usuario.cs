namespace Nido.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Usuario(Guid id, string nombre, string email, string sexo, string? telefono = null, string? fotoStorageKey = null, DateTime createdAt = default, DateTime? fotoUpdatedAt = null)
    {
        Id = id;
        Nombre = nombre;
        Email = email;
        Sexo = sexo;
        Telefono = telefono;
        FotoStorageKey = fotoStorageKey;
        CreatedAt = createdAt;
        FotoUpdatedAt = fotoUpdatedAt;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Sexo { get; set; } = string.Empty;

    public string? Telefono { get; set; } = string.Empty;

    public string? FotoStorageKey { get; set; }

    public DateTime? FotoUpdatedAt { get; set; }

    public DateTime CreatedAt { get; private set; }


    public void ActualizarPerfil(string nombre, string sexo, string? telefono = null, string? fotoStorageKey = null, DateTime? fotoUpdatedAt = null)
    {
        Nombre = nombre;
        Sexo = sexo;
        Telefono = telefono;
        FotoStorageKey = fotoStorageKey;
        FotoUpdatedAt = fotoUpdatedAt;
    }
}
