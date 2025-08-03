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
}