using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

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
            .Include(x => x.ToAccount)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.Type == OperationType.Income, cancellationToken);

        if (operation is null)
            return TypedResults.Problem(
                title: "Income operation not found",
                detail: $"Income operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var account = operation.ToAccount;
        if (account is not null)
            account.Balance -= operation.Amount;

        dbContext.Operations.Remove(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}