namespace Nido.Infrastructure.Persistence.Entities;

public partial class OnboardingGoal
{
    public Guid Id { get; set; }
    public Guid HogarId { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Descripcion { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
}
