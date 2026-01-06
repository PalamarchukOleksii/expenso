using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseOperations;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public class Request
    {
        [Required(ErrorMessage = "AccountId is required.")]
        public Guid AccountId { get; set; }

        [Required(ErrorMessage = "CategoryId is required.")]
        public Guid CategoryId { get; set; }

        [Range(0.0000001, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    public record Response(
        Guid Id,
        Guid AccountId,
        Guid CategoryId,
        decimal Amount,
        string Currency,
        DateTime Timestamp,
        string? Note);

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var account = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.AccountId, cancellationToken);

        if (account is null)
            return TypedResults.Problem(
                title: "Account Not Found",
                detail: $"Account with ID '{request.AccountId}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var categoryExist = await dbContext.Categories.AnyAsync(x =>
            x.Id == request.CategoryId &&
            (x.UserId == userId || x.IsDefault) &&
            x.Type == CategoryType.Expense, cancellationToken);

        if (!categoryExist)
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Expense category with ID '{request.CategoryId}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var operation = new Operation
        {
            UserId = userId,
            FromAccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Currency = account.Currency,
            Type = OperationType.Expense,
            Note = request.Note
        };

        account.Balance -= request.Amount;

        dbContext.Operations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{EndpointRoutes.Prefix}/{EndpointRoutes.Segments.ExpenseOperations}/{operation.Id}";

        var response = new Response(
            operation.Id,
            operation.FromAccountId!.Value,
            operation.CategoryId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note
        );

        return TypedResults.Created(location, response);
    }
}