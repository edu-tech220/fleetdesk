namespace FleetDesk.Api.Domain;

/// El usuario autenticado no tiene permiso sobre el recurso solicitado; HTTP 403.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
