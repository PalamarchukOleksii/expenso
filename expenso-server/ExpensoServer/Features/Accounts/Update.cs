using System.Security.Claims;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Common.Api.Filters;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using ExpensoServer.Features.Users;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch("{id:guid}", HandleAsync)
                .AddEndpointFilter<RequestValidationFilter<Request>>();
        }
    }

    public record Request(string? Name, decimal? Balance, string? Currency);

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters long.")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters long.")
                .When(x => x.Name is not null);

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0).WithMessage("Balance must be zero or positive.")
                .When(x => x.Balance.HasValue);

            RuleFor(x => x.Currency)
                .Must(value => Enum.TryParse<Currency>(value, false, out _))
                .WithMessage("Invalid currency.")
                .When(x => x.Currency is not null);
        }
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(request.Currency, ignoreCase: false, out var currencyEnum))
            return TypedResults.BadRequest();
        
        var userId = claimsPrincipal.GetUserId();

        var account =
            await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        if (account == null)
            return TypedResults.NotFound();

        if (request.Name is not null && request.Name != account.Name)
        {
            var existingAccount =
                await dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == userId && x.Name == request.Name,
                    cancellationToken);
            if (existingAccount is not null)
                return TypedResults.Conflict();
        }

        if (request.Name is not null) account.Name = request.Name;
        if (request.Balance is not null) account.Balance = request.Balance.Value;
        if (request.Currency is not null) account.Currency = currencyEnum;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}