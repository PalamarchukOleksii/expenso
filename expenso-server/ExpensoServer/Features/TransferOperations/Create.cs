using System.Security.Claims;
using System.Text.Json;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>();
        }
    }

    public record Request(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Note);

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
                .NotEmpty().WithMessage("FromAccountId is required.");

            RuleFor(x => x.ToAccountId)
                .NotEmpty().WithMessage("ToAccountId is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        }
    }

    private static readonly HttpClient _httpClient = new();

    private static async Task<IResult> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var fromAccount = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.FromAccountId, cancellationToken);

        if (fromAccount is null)
            return TypedResults.NotFound();

        var toAccount = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.ToAccountId, cancellationToken);

        if (toAccount is null)
            return TypedResults.NotFound();

        decimal? exchangeRate = null;
        var convertedAmount = request.Amount;

        if (fromAccount.Currency != toAccount.Currency)
        {
            exchangeRate = await GetExchangeRateAsync(fromAccount.Currency.ToString(), toAccount.Currency.ToString(),
                cancellationToken);
            convertedAmount *= exchangeRate.Value;
        }

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += convertedAmount;

        var operation = new Operation
        {
            UserId = userId,
            FromAccountId = fromAccount.Id,
            ToAccountId = toAccount.Id,
            Amount = request.Amount,
            Currency = fromAccount.Currency,
            ConvertedAmount = exchangeRate.HasValue ? convertedAmount : null,
            ConvertedCurrency = exchangeRate.HasValue ? toAccount.Currency : null,
            ExchangeRate = exchangeRate,
            Type = OperationType.Transfer,
            Note = request.Note,
        };

        dbContext.Operations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{Routes.Prefix}/{Routes.Segments.TransferOperations}/{operation.Id}";

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

        return TypedResults.Created(location, response);
    }

    private static async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var url = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{fromCurrency.ToLower()}.json";
        var json = await _httpClient.GetStringAsync(url, cancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty(fromCurrency.ToLower(), out var ratesElement))
            throw new InvalidOperationException($"Missing base currency '{fromCurrency}' in response.");

        if (!ratesElement.TryGetProperty(toCurrency.ToLower(), out var rateElement))
            throw new InvalidOperationException($"Exchange rate not found for '{fromCurrency}' → '{toCurrency}'.");

        return rateElement.GetDecimal();
    }
}