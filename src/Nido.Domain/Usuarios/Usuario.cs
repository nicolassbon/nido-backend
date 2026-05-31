namespace Nido.Domain.Usuarios;

public sealed class Usuario
{
    private Usuario()
    {
    }

    public Usuario(Guid id, string nombre, string email, string sexo, string? fotoUrl = null)
    {
        Id = id;
        Nombre = nombre;
        Email = email;
        Sexo = sexo;
        FotoUrl = fotoUrl;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Sexo { get; set; } = string.Empty;

    public string? FotoUrl { get; set; }

  
    public void ActualizarPerfil(string nombre, string sexo, string? fotoUrl = null)
    {
        Nombre = nombre;
        Sexo = sexo;
        FotoUrl = fotoUrl;
    }
}

