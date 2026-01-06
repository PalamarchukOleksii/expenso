using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.TransferOperations;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    [CannotTransferToSameAccountIfBothProvided(ErrorMessage = "Cannot transfer to the same account.")]
    public class Request
    {
        [GuidIfHasValue(ErrorMessage = "FromAccountId is required.")]
        public Guid? FromAccountId { get; set; }

        [GuidIfHasValue(ErrorMessage = "ToAccountId is required.")]
        public Guid? ToAccountId { get; set; }

        [DecimalGreaterThanZeroIfHasValue(ErrorMessage = "Amount must be greater than zero.")]
        public decimal? Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        [DecimalGreaterThanZeroIfHasValue(ErrorMessage = "ExchangeRate must be greater than zero if provided.")]
        public decimal? ExchangeRate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class GuidIfHasValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Guid guid && guid == Guid.Empty)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DecimalGreaterThanZeroIfHasValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal decimalValue && decimalValue <= 0)
                return new ValidationResult(ErrorMessage);

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CannotTransferToSameAccountIfBothProvidedAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Request request)
            {
                if (request.FromAccountId.HasValue
                    && request.ToAccountId.HasValue
                    && request.FromAccountId.Value == request.ToAccountId.Value)
                {
                    return new ValidationResult(
                        ErrorMessage,
                        new[] { nameof(Request.FromAccountId), nameof(Request.ToAccountId) }
                    );
                }
            }

            return ValidationResult.Success;
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

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Guid id,
        Request request,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
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
            return TypedResults.Problem(
                title: "Transfer Operation Not Found",
                detail: $"Transfer operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

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
                return TypedResults.Problem(
                    title: "From Account Not Found",
                    detail:
                    $"Account with ID '{request.FromAccountId ?? operation.FromAccountId}' was not found for the current user.",
                    statusCode: StatusCodes.Status404NotFound);

            var newToAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(
                    a => a.Id == (request.ToAccountId ?? operation.ToAccountId) && a.UserId == userId,
                    cancellationToken);
            if (newToAccount is null)
                return TypedResults.Problem(
                    title: "To Account Not Found",
                    detail:
                    $"Account with ID '{request.ToAccountId ?? operation.ToAccountId}' was not found for the current user.",
                    statusCode: StatusCodes.Status404NotFound);

            var requiresConversion = newFromAccount.Currency != newToAccount.Currency;
            var convertedAmount = newAmount;

            if (requiresConversion)
            {
                if (!request.ExchangeRate.HasValue)
                    return TypedResults.Problem(
                        title: "Missing Exchange Rate",
                        detail:
                        "ExchangeRate must be provided when transferring between accounts with different currencies.",
                        statusCode: StatusCodes.Status400BadRequest);

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
                    return TypedResults.Problem(
                        title: "Missing Exchange Rate",
                        detail:
                        "ExchangeRate must be provided when transferring between accounts with different currencies.",
                        statusCode: StatusCodes.Status400BadRequest);

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

        if (request.Note is not null && request.Note != operation.Note)
            operation.Note = request.Note;

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