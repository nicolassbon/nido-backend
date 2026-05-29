using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Usuario
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? OauthProvider { get; set; }

    public string? OauthId { get; set; }

    public string Sexo { get; set; } = null!;

    public string? FotoUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AsignacionesTarea> AsignacionesTareas { get; set; } = new List<AsignacionesTarea>();

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<InvitacionesHogar> InvitacionesHogars { get; set; } = new List<InvitacionesHogar>();

    public virtual ICollection<ListaCompra> ListaCompras { get; set; } = new List<ListaCompra>();

    public virtual ICollection<LogrosUsuario> LogrosUsuarios { get; set; } = new List<LogrosUsuario>();

    public virtual ICollection<MiembrosHogar> MiembrosHogars { get; set; } = new List<MiembrosHogar>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<RecetasCocinada> RecetasCocinada { get; set; } = new List<RecetasCocinada>();

    public virtual ICollection<ReseniasRecetum> ReseniasReceta { get; set; } = new List<ReseniasRecetum>();

    public virtual ICollection<RestriccionesUsuario> RestriccionesUsuarios { get; set; } = new List<RestriccionesUsuario>();

    public virtual ICollection<OnboardingState> OnboardingStates { get; set; } = new List<OnboardingState>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<StockHogar> StockHogarCargadoPorNavigations { get; set; } = new List<StockHogar>();

    public virtual ICollection<StockHogar> StockHogarUpdatedByNavigations { get; set; } = new List<StockHogar>();

    public virtual ICollection<Tarea> TareaCompletadoPorNavigations { get; set; } = new List<Tarea>();

    public virtual ICollection<Tarea> TareaCreadoPorNavigations { get; set; } = new List<Tarea>();
}
