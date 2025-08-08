using System.Security.Claims;
using ExpensoServer.Common.Endpoints;
using ExpensoServer.Common.Endpoints.Constants;
using ExpensoServer.Common.Endpoints.Extensions;
using ExpensoServer.Common.Endpoints.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.IncomeOperations;

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

    public record Request(Guid AccountId, Guid CategoryId, decimal Amount, string? Note);

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
                .NotEmpty().WithMessage("AccountId is required.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        }
    }

    private static async Task<IResult> HandleAsync(Request request, ClaimsPrincipal claimsPrincipal,
        ApplicationDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var account = await dbContext.Accounts.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Id == request.AccountId, cancellationToken);

        if (account is null)
            return TypedResults.NotFound();

        var categoryExist = await dbContext.Categories.AnyAsync(x =>
            x.Id == request.CategoryId &&
            (x.UserId == userId || x.IsDefault) &&
            x.Type == CategoryType.Income, cancellationToken);

        if (!categoryExist)
            return TypedResults.NotFound();

        var operation = new Operation
        {
            UserId = userId,
            ToAccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Currency = account.Currency,
            Type = OperationType.Income,
            Note = request.Note
        };

        account.Balance += request.Amount;

        dbContext.Operations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{Routes.Prefix}/{Routes.Segments.IncomeOperations}/{operation.Id}";
        var response = new Response(
            operation.Id,
            operation.ToAccountId!.Value,
            operation.CategoryId!.Value,
            operation.Amount,
            operation.Currency.ToString(),
            operation.Timestamp,
            operation.Note
        );

        return TypedResults.Created(location, response);
    }
}