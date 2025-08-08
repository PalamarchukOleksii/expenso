using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync)
                .Produces<Response[]>();
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

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operations = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Income)
            .Select(x => new Response(
                x.Id,
                x.ToAccountId!.Value,
                x.CategoryId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(operations);
    }
}