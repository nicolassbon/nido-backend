namespace Nido.Infrastructure.Persistence.Entities;

public partial class OnboardingState
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid HogarId { get; set; }
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public bool Step2Skipped { get; set; }
    public DateTime? Step3CompletedAt { get; set; }
    public bool Step3Skipped { get; set; }
    public DateTime? Step4CompletedAt { get; set; }
    public bool Step4Skipped { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Hogare Hogar { get; set; } = null!;
}
