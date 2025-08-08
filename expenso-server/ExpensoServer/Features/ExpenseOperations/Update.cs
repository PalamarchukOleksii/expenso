using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.ExpenseOperations;

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

    public record Request(Guid? AccountId, Guid? CategoryId, decimal? Amount, string? Note);

    public record Response(
        Guid Id,
        Guid AccountId,
        Guid CategoryId,
        decimal Amount,
        string Currency,
        DateTime Timestamp,
        string? Note);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("AccountId is required.")
                .When(x => x.AccountId.HasValue);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId is required.")
                .When(x => x.CategoryId.HasValue);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.")
                .When(x => x.Amount.HasValue);

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.")
                .When(x => x.Note is not null);
        }
    }

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

        var oldAccount = operation.FromAccount!;
        var oldAmount = operation.Amount;
        var newAmount = request.Amount ?? oldAmount;

        if (request.AccountId.HasValue && request.AccountId != operation.FromAccountId)
        {
            var newAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);

            if (newAccount is null)
                return TypedResults.Problem(
                    title: "Not Found",
                    detail: $"Account with ID '{request.AccountId}' was not found.",
                    statusCode: StatusCodes.Status404NotFound);

            oldAccount.Balance += oldAmount;
            newAccount.Balance -= newAmount;

            operation.FromAccountId = newAccount.Id;
            operation.Currency = newAccount.Currency;
            operation.FromAccount = newAccount;
        }
        else if (request.Amount.HasValue && request.Amount != oldAmount)
        {
            var diff = newAmount - oldAmount;
            oldAccount.Balance -= diff;
            operation.Amount = newAmount;
        }

        if (request.CategoryId.HasValue && request.CategoryId != operation.CategoryId)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == userId || c.IsDefault) &&
                c.Type == CategoryType.Expense, cancellationToken);

            if (!categoryExists)
                return TypedResults.Problem(
                    title: "Not Found",
                    detail: $"Expense category with ID '{request.CategoryId}' was not found.",
                    statusCode: StatusCodes.Status404NotFound);

            operation.CategoryId = request.CategoryId;
        }

        if (request.Note is not null && request.Note != operation.Note)
            operation.Note = request.Note;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new Response(
            operation.Id,
            operation.FromAccountId!.Value,
            operation.CategoryId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note
        );

        return TypedResults.Ok(response);
    }
}