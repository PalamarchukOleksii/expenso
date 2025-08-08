using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseOperations;

public static class GetById
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/{id:guid}", HandleAsync)
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public record Response(
        Guid Id,
        Guid AccountId,
        Guid CategoryId,
        decimal Amount,
        string Currency,
        DateTime Timestamp,
        string? Note);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var response = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Expense)
            .Select(x => new Response(
                x.Id,
                x.FromAccountId!.Value,
                x.CategoryId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (response is null)
            return TypedResults.Problem(
                title: "Expense operation Not Found",
                detail: $"Expense operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        return TypedResults.Ok(response);
    }
}