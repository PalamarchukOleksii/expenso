using ExpensoServer.Common.Api.Filters;

namespace ExpensoServer.Common.Api.Extensions;

public static class EndpointExtensions
{
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder
            .AddEndpointFilter<RequestValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
    
    public static RouteHandlerBuilder WithUserId(this RouteHandlerBuilder builder)
    {
        return builder
            .AddEndpointFilter<RequireUserIdFilter>()
            .Produces(StatusCodes.Status401Unauthorized);
    }
    
    public static RouteGroupBuilder WithUserId(this RouteGroupBuilder builder)
    {
        return builder
            .AddEndpointFilter<RequireUserIdFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}