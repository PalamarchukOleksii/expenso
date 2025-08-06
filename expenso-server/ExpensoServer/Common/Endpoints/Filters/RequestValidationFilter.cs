using FluentValidation;

namespace ExpensoServer.Common.Endpoints.Filters;

public class RequestValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().First();

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid) return TypedResults.ValidationProblem(validationResult.ToDictionary());

        return await next(context);
    }
}