using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Data.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Accounts;

public static class Create
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/create", HandleAsync)
                .ProducesValidationProblem()
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }
    }

    public class Request
    {
        [Required(ErrorMessage = "Name is required.")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters long.")]
        [MaxLength(50, ErrorMessage = "Name must be at most 50 characters long.")]
        public required string Name { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Balance must be zero or positive.")]
        public decimal Balance { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [CurrencyValidation]
        public required string Currency { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CurrencyValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string strValue)
                return ValidationResult.Success;

            if (!Enum.TryParse<Currency>(strValue, ignoreCase: true, out _))
                return new ValidationResult("Invalid currency.");

            return ValidationResult.Success;
        }
    }

    public record Response(Guid Id, string Name, decimal Balance, string Currency);

    private static async Task<Results<Created<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        ApplicationDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Currency>(request.Currency, false, out var currencyEnum))
            return TypedResults.Problem(
                title: "Invalid Currency",
                detail: $"The currency '{request.Currency}' is not supported.",
                statusCode: StatusCodes.Status400BadRequest);

        var userId = claimsPrincipal.GetUserId();

        var nameExist = await dbContext.Accounts
            .AnyAsync(x => x.UserId == userId && x.Name == request.Name, cancellationToken);

        if (nameExist)
            return TypedResults.Problem(
                title: "Account Name Already Exists",
                detail: $"An account with the name '{request.Name}' already exists for this user.",
                statusCode: StatusCodes.Status409Conflict);

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Balance = request.Balance,
            Currency = currencyEnum
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var location =
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{EndpointRoutes.Prefix}/{EndpointRoutes.Segments.Accounts}/{account.Id}";

        return TypedResults.Created(location,
            new Response(account.Id, account.Name, account.Balance, account.Currency.ToString()));
    }
}