namespace ExpensoServer.Common.Api.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(
        this WebApplication app,
        string routePrefix = "")
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder routeBuilder = string.IsNullOrWhiteSpace(routePrefix)
            ? app
            : app.MapGroup(routePrefix);

        foreach (var endpoint in endpoints) 
            endpoint.MapEndpoint(routeBuilder);
    }
}