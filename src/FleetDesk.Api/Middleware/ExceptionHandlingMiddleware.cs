using FleetDesk.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Api.Middleware;

/// Traduce las excepciones de dominio a respuestas ProblemDetails consistentes.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Acceso no permitido"),
                BusinessRuleException => (StatusCodes.Status409Conflict, "Regla de negocio violada"),
                ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia"),
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? "Ocurrio un error inesperado." : ex.Message,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
