using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync);
        }
    }

    public record Response(
        Guid Id,
        Guid FromAccountId,
        Guid ToAccountId,
        decimal Amount,
        string Currency,
        DateTime Timestamp,
        string? Note,
        decimal? ConvertedAmount = null,
        string? ConvertedCurrency = null,
        decimal? ExchangeRate = null
    );

    private static async Task<IResult> HandleAsync(ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operations = await dbContext.Operations
            .Where(x => x.UserId == userId && x.Type == OperationType.Transfer)
            .Select(x => new Response(
                x.Id,
                x.FromAccountId!.Value,
                x.ToAccountId!.Value,
                x.Amount,
                x.Currency.ToString(),
                x.Timestamp,
                x.Note,
                x.ConvertedAmount,
                x.ConvertedCurrency!.Value.ToString(),
                x.ExchangeRate!.Value))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(operations);
    }
}