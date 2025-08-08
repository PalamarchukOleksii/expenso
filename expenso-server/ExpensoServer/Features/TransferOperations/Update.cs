using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>();
        }
    }

    public record Request(
        Guid? FromAccountId,
        Guid? ToAccountId,
        decimal? Amount,
        string? Note,
        decimal? ExchangeRate
    );

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

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FromAccountId)
                .NotEmpty().When(x => x.FromAccountId.HasValue);

            RuleFor(x => x.ToAccountId)
                .NotEmpty().When(x => x.ToAccountId.HasValue);

            RuleFor(x => x)
                .Must(x => x.FromAccountId != x.ToAccountId)
                .When(x => x.FromAccountId.HasValue && x.ToAccountId.HasValue)
                .WithMessage("Cannot transfer to the same account.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).When(x => x.Amount.HasValue);

            RuleFor(x => x.Note)
                .MaximumLength(500).When(x => x.Note is not null);

            RuleFor(x => x.ExchangeRate)
                .GreaterThan(0).When(x => x.ExchangeRate.HasValue);
        }
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Request request,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operation = await dbContext.Operations
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.Type == OperationType.Transfer, cancellationToken);

        if (operation is null)
            return TypedResults.NotFound();

        var oldFromAccount = operation.FromAccount!;
        var oldToAccount = operation.ToAccount!;
        var oldAmount = operation.Amount;
        var oldConvertedAmount = operation.ConvertedAmount ?? operation.Amount;
        var newAmount = request.Amount ?? oldAmount;

        if ((request.FromAccountId.HasValue && request.FromAccountId != operation.FromAccountId) ||
            (request.ToAccountId.HasValue && request.ToAccountId != operation.ToAccountId))
        {
            oldFromAccount.Balance += oldAmount;
            oldToAccount.Balance -= oldConvertedAmount;

            var newFromAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(
                    a => a.Id == (request.FromAccountId ?? operation.FromAccountId) && a.UserId == userId,
                    cancellationToken);
            if (newFromAccount is null)
                return TypedResults.NotFound();

            var newToAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == (request.ToAccountId ?? operation.ToAccountId) && a.UserId == userId,
                    cancellationToken);
            if (newToAccount is null)
                return TypedResults.NotFound();

            var requiresConversion = newFromAccount.Currency != newToAccount.Currency;
            var convertedAmount = newAmount;

            if (requiresConversion)
            {
                if (!request.ExchangeRate.HasValue)
                    return TypedResults.BadRequest();
                convertedAmount *= request.ExchangeRate.Value;
            }

            newFromAccount.Balance -= newAmount;
            newToAccount.Balance += convertedAmount;

            operation.FromAccountId = newFromAccount.Id;
            operation.ToAccountId = newToAccount.Id;
            operation.FromAccount = newFromAccount;
            operation.ToAccount = newToAccount;
            operation.Amount = newAmount;
            operation.Currency = newFromAccount.Currency;
            operation.ConvertedAmount = requiresConversion ? convertedAmount : null;
            operation.ConvertedCurrency = requiresConversion ? newToAccount.Currency : null;
            operation.ExchangeRate = requiresConversion ? request.ExchangeRate : null;
        }
        else if (request.Amount.HasValue && request.Amount != oldAmount)
        {
            var requiresConversion = oldFromAccount.Currency != oldToAccount.Currency;
            var convertedAmount = newAmount;

            if (requiresConversion)
            {
                if (!request.ExchangeRate.HasValue)
                    return TypedResults.BadRequest();
                convertedAmount = newAmount * request.ExchangeRate.Value;
            }

            oldFromAccount.Balance += oldAmount;
            oldFromAccount.Balance -= newAmount;

            oldToAccount.Balance -= oldConvertedAmount;
            oldToAccount.Balance += convertedAmount;

            operation.Amount = newAmount;
            operation.ConvertedAmount = requiresConversion ? convertedAmount : null;
            operation.ExchangeRate = requiresConversion ? request.ExchangeRate : null;
        }

        if (request.Note is not null && request.Note != operation.Note) operation.Note = request.Note;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(
            operation.Id,
            operation.FromAccountId!.Value,
            operation.ToAccountId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note,
            operation.ConvertedAmount,
            operation.ConvertedCurrency?.ToString(),
            operation.ExchangeRate
        );

        return TypedResults.Ok(response);
    }
}