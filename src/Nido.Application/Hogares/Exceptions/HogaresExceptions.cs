using Nido.Domain.Exceptions;

namespace Nido.Application.Hogares.Exceptions;

public sealed class MissingInvitationTokenException : NidoException
{
    public MissingInvitationTokenException() : base("MISSING_INVITATION_TOKEN", "El token de invitación es requerido.") { }
}

public sealed class InvitationNotFoundException : NidoException
{
    public InvitationNotFoundException() : base("INVITATION_NOT_FOUND", "Invitación no encontrada o inválida.") { }
}

public sealed class InvitationAlreadyProcessedException : NidoException
{
    public InvitationAlreadyProcessedException() : base("INVITATION_ALREADY_PROCESSED", "Esta invitación ya fue utilizada o está cancelada.") { }
}

public sealed class InvitationExpiredException : NidoException
{
    public InvitationExpiredException() : base("INVITATION_EXPIRED", "La invitación ha expirado.") { }
}

public sealed class MaxMembersExceededException : NidoException
{
    public MaxMembersExceededException(int max) : base("MAX_MEMBERS_EXCEEDED", $"El hogar ya alcanzó el límite de {max} miembros.") { }
}

public sealed class AlreadyMemberException : NidoException
{
    public AlreadyMemberException() : base("ALREADY_MEMBER", "Ya sos miembro de este hogar.") { }
}

public sealed class NotHouseholdOwnerException : NidoException
{
    public NotHouseholdOwnerException() : base("NOT_HOUSEHOLD_OWNER", "Solo el propietario puede invitar integrantes.") { }
}

public sealed class NotHouseholdMemberException : NidoException
{
    public NotHouseholdMemberException() : base("NOT_HOUSEHOLD_MEMBER", "El usuario no pertenece a ningún hogar.") { }
}

public sealed class NotSoleOwnerException : NidoException
{
    public NotSoleOwnerException() : base("NOT_SOLE_OWNER", "Ya pertenecés a un hogar con otros miembros. No podés unirte a otro hogar.") { }
}

public sealed class CannotRemoveSelfException : NidoException
{
    public CannotRemoveSelfException() : base("CANNOT_REMOVE_SELF", "No podés eliminarte a vos mismo del hogar.") { }
}
