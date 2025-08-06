using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Features;
using ExpensoServer.Features.Auth;

namespace ExpensoServer;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup(Routes.Prefix);

        endpoints.MapUserEndpoints();
        endpoints.MapAuthEndpoints();
        endpoints.MapAccountEndpoints();
        endpoints.MapIncomeCategoryEndpoints();
        endpoints.MapExpenseCategoryEndpoints();
    }

    private static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Routes.Segments.Users)
            .WithTags(Routes.Segments.Users);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Users.GetCurrent.Endpoints>()
            .MapEndpoint<Features.Users.Update.Endpoint>()
            .MapEndpoint<Features.Users.Delete.Endpoint>();
    }

    private static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Routes.Segments.Auth)
            .WithTags(Routes.Segments.Auth);

        endpoints.MapPublicGroup()
            .MapEndpoint<Features.Auth.Register.Endpoint>()
            .MapEndpoint<Features.Auth.Login.Endpoint>();

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Auth.Logout.Endpoint>();
    }

    private static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Routes.Segments.Accounts)
            .WithTags(Routes.Segments.Accounts);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Accounts.Create.Endpoint>()
            .MapEndpoint<Features.Accounts.Update.Endpoint>()
            .MapEndpoint<Features.Accounts.Delete.Endpoint>()
            .MapEndpoint<Features.Accounts.GetById.Endpoint>()
            .MapEndpoint<Features.Accounts.GetAll.Endpoint>();
    }

    private static void MapIncomeCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Routes.Segments.IncomeCategories)
            .WithTags(Routes.Segments.IncomeCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.IncomeCategories.Create.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Update.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Delete.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetById.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetAll.Endpoint>();
    }

    private static void MapExpenseCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(Routes.Segments.ExpenseCategories)
            .WithTags(Routes.Segments.ExpenseCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.ExpenseCategories.Create.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.Update.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.Delete.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.GetById.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.GetAll.Endpoint>();
    }

    private static RouteGroupBuilder MapPublicGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .AllowAnonymous();
    }

    private static RouteGroupBuilder MapAuthorizedGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .RequireAuthorization();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}