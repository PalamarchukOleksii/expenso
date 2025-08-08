using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ExpensoServer;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Prefix);

        endpoints.MapUserEndpoints();
        endpoints.MapAuthEndpoints();
        endpoints.MapAccountEndpoints();
        endpoints.MapIncomeCategoryEndpoints();
        endpoints.MapExpenseCategoryEndpoints();
        endpoints.MapIncomeOperationsEndpoints();
        endpoints.MapExpenseOperationsEndpoints();
        endpoints.MapTransferOperationsEndpoints();
        endpoints.MapExchangeRateEndpoints();
    }

    private static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.Users)
            .WithTags(EndpointRoutes.Segments.Users);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Users.GetCurrent.Endpoint>()
            .MapEndpoint<Features.Users.Update.Endpoint>()
            .MapEndpoint<Features.Users.Delete.Endpoint>();
    }

    private static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.Auth)
            .WithTags(EndpointRoutes.Segments.Auth);

        endpoints.MapPublicGroup()
            .MapEndpoint<Features.Auth.Register.Endpoint>()
            .MapEndpoint<Features.Auth.Login.Endpoint>();

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Auth.Logout.Endpoint>();
    }

    private static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.Accounts)
            .WithTags(EndpointRoutes.Segments.Accounts);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.Accounts.Create.Endpoint>()
            .MapEndpoint<Features.Accounts.Update.Endpoint>()
            .MapEndpoint<Features.Accounts.Delete.Endpoint>()
            .MapEndpoint<Features.Accounts.GetById.Endpoint>()
            .MapEndpoint<Features.Accounts.GetAll.Endpoint>();
    }

    private static void MapIncomeCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.IncomeCategories)
            .WithTags(EndpointRoutes.Segments.IncomeCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.IncomeCategories.Create.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Update.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.Delete.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetById.Endpoint>()
            .MapEndpoint<Features.IncomeCategories.GetAll.Endpoint>();
    }

    private static void MapExpenseCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.ExpenseCategories)
            .WithTags(EndpointRoutes.Segments.ExpenseCategories);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.ExpenseCategories.Create.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.Update.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.Delete.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.GetById.Endpoint>()
            .MapEndpoint<Features.ExpenseCategories.GetAll.Endpoint>();
    }

    private static void MapIncomeOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.IncomeOperations)
            .WithTags(EndpointRoutes.Segments.IncomeOperations);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.IncomeOperations.Create.Endpoint>()
            .MapEndpoint<Features.IncomeOperations.Update.Endpoint>()
            .MapEndpoint<Features.IncomeOperations.Delete.Endpoint>()
            .MapEndpoint<Features.IncomeOperations.GetById.Endpoint>()
            .MapEndpoint<Features.IncomeOperations.GetAll.Endpoint>();
    }

    private static void MapExpenseOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.ExpenseOperations)
            .WithTags(EndpointRoutes.Segments.ExpenseOperations);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.ExpenseOperations.Create.Endpoint>()
            .MapEndpoint<Features.ExpenseOperations.Update.Endpoint>()
            .MapEndpoint<Features.ExpenseOperations.Delete.Endpoint>()
            .MapEndpoint<Features.ExpenseOperations.GetById.Endpoint>()
            .MapEndpoint<Features.ExpenseOperations.GetAll.Endpoint>();
    }

    private static void MapExchangeRateEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.ExchangeRates)
            .WithTags(EndpointRoutes.Segments.ExchangeRates);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.ExchangeRates.Get.Endpoint>();
    }

    private static void MapTransferOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(EndpointRoutes.Segments.TransferOperations)
            .WithTags(EndpointRoutes.Segments.TransferOperations);

        endpoints.MapAuthorizedGroup()
            .MapEndpoint<Features.TransferOperations.Create.Endpoint>()
            .MapEndpoint<Features.TransferOperations.Update.Endpoint>()
            .MapEndpoint<Features.TransferOperations.Delete.Endpoint>()
            .MapEndpoint<Features.TransferOperations.GetById.Endpoint>()
            .MapEndpoint<Features.TransferOperations.GetAll.Endpoint>();
    }

    private static RouteGroupBuilder MapPublicGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .AllowAnonymous();
    }

    private static RouteGroupBuilder MapAuthorizedGroup(this IEndpointRouteBuilder app, string? prefix = null)
    {
        return app.MapGroup(prefix ?? string.Empty)
            .RequireAuthorization()
            .WithMetadata(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}