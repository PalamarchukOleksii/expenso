using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

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
            .Include(x => x.ToAccount)
            .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == userId &&
                    x.Type == OperationType.Income,
                cancellationToken);

        if (operation is null)
            return TypedResults.NotFound();

        var oldAccount = operation.ToAccount!;
        var oldAmount = operation.Amount;
        var newAmount = request.Amount ?? oldAmount;

        if (request.AccountId.HasValue && request.AccountId != operation.ToAccountId)
        {
            var newAccount = await dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, cancellationToken);

            if (newAccount is null)
                return TypedResults.NotFound();

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
                return TypedResults.NotFound();

            operation.CategoryId = request.CategoryId;
        }

        if (request.Note is not null && request.Note != operation.Note) operation.Note = request.Note;

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