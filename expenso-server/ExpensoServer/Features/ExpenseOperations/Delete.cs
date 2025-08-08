using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseOperations;

public static class Delete
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete("/{id:guid}", HandleAsync)
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operation = await dbContext.Operations
            .Include(x => x.FromAccount)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.Type == OperationType.Expense, cancellationToken);

        if (operation is null)
            return TypedResults.Problem(
                title: "Not Found",
                detail: $"Expense operation with ID '{id}' was not found.",
                statusCode: StatusCodes.Status404NotFound);

        var account = operation.FromAccount;
        if (account is not null) account.Balance += operation.Amount;

        dbContext.Operations.Remove(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}