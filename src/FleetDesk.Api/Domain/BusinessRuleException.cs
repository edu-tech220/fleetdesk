namespace FleetDesk.Api.Domain;

/// Violacion de una regla de negocio; el middleware la traduce a HTTP 409 (Conflict).
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// El token de concurrencia enviado por el cliente no coincide con el estado actual; HTTP 409.
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}

/// Entidad solicitada no encontrada; el middleware la traduce a HTTP 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
