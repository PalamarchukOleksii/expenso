using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("/{id:guid}", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public class Request
    {
        [GuidRequiredIfHasValue(ErrorMessage = "AccountId is required.")]
        public Guid? AccountId { get; set; }

        [GuidRequiredIfHasValue(ErrorMessage = "CategoryId is required.")]
        public Guid? CategoryId { get; set; }

        [DecimalGreaterThanZeroIfHasValue(ErrorMessage = "Amount must be greater than zero.")]
        public decimal? Amount { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class GuidRequiredIfHasValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is Guid guid)
            {
                if (guid == Guid.Empty)
                    return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DecimalGreaterThanZeroIfHasValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue <= 0)
                    return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
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
        Request request,
        ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var operation = await dbContext.Operations
            .Include(x => x.ToAccount)
            .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId &&
                    x.Type == OperationType.Income,
                cancellationToken);

        if (operation is null)
            return TypedResults.Problem(
                title: "Income operation not found",
                detail: $"Income operation with ID '{id}' was not found for the current user.",
                statusCode: StatusCodes.Status404NotFound);

        var oldAccount = operation.ToAccount!;
        var oldAmount = operation.Amount;
        var newAmount = request.Amount ?? oldAmount;

        if (request.AccountId.HasValue && request.AccountId != operation.ToAccountId)
        {
            var newAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);

            if (newAccount is null)
                return TypedResults.Problem(
                    title: "Account not found",
                    detail: $"Account with ID '{request.AccountId}' was not found for the current user.",
                    statusCode: StatusCodes.Status404NotFound);

            oldAccount.Balance -= oldAmount;
            newAccount.Balance += newAmount;

            operation.ToAccountId = newAccount.Id;
            operation.Currency = newAccount.Currency;
            operation.ToAccount = newAccount;
        }
        else if (request.Amount.HasValue && request.Amount != oldAmount)
        {
            var diff = newAmount - oldAmount;
            oldAccount.Balance += diff;
            operation.Amount = newAmount;
        }

        if (request.CategoryId.HasValue && request.CategoryId != operation.CategoryId)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == userId || c.IsDefault) &&
                c.Type == CategoryType.Income, cancellationToken);

            if (!categoryExists)
                return TypedResults.Problem(
                    title: "Category not found",
                    detail: $"Income category with ID '{request.CategoryId}' was not found for the current user.",
                    statusCode: StatusCodes.Status404NotFound);

            operation.CategoryId = request.CategoryId;
        }

        if (request.Note is not null && request.Note != operation.Note)
            operation.Note = request.Note;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(
            operation.Id,
            operation.ToAccountId!.Value,
            operation.CategoryId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note
        );

        return TypedResults.Ok(response);
    }
}