using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Terminar.SharedKernel;

namespace Terminar.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failure: {Errors}", ex.Errors);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(
                ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Type = "https://terminar.app/errors/validation-failed",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Not found: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/not-found",
                Title = "Resource not found.",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (ConflictException ex)
        {
            logger.LogWarning("Conflict: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/conflict",
                Title = "Conflict.",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (ForbiddenException ex)
        {
            logger.LogWarning("Forbidden: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/forbidden",
                Title = "Forbidden.",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid argument: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/unprocessable",
                Title = "Request cannot be processed.",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (UnprocessableException ex)
        {
            logger.LogWarning("Unprocessable: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/unprocessable",
                Title = "Request cannot be processed.",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://terminar.app/errors/internal",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}

// Exceptions defined in Terminar.SharedKernel.Exceptions
