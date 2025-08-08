using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

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
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operation = await dbContext.Operations
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId &&
                    x.Type == OperationType.Transfer,
                cancellationToken);

        if (operation is null)
            return TypedResults.Problem(
                title: "Operation Not Found",
                detail: $"Operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var fromAccount = operation.FromAccount;
        if (fromAccount is not null)
            fromAccount.Balance += operation.Amount;

        var toAccount = operation.ToAccount;
        if (toAccount is not null)
            toAccount.Balance -= operation.ConvertedAmount ?? operation.Amount;

        dbContext.Operations.Remove(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}