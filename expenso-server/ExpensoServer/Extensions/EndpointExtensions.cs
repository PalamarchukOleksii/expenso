using System.Reflection;
using ExpensoServer.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpensoServer.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointServiceDescriptors = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(endpointServiceDescriptors);

        return services;
    }

    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        string routePrefix = "")
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder routeBuilder = string.IsNullOrWhiteSpace(routePrefix)
            ? app
            : app.MapGroup(routePrefix);

        foreach (var endpoint in endpoints) 
            endpoint.MapEndpoint(routeBuilder);

        return app;
    }
}