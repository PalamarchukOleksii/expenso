using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Api;
using ExpensoServer.Common.Api.Extensions;
using ExpensoServer.Data;
using ExpensoServer.Data.Entities;
using ExpensoServer.Features.Users.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Login
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", HandleAsync)
                .WithRequestValidation<Request>()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .Produces<Response>();
        }
    }

    public record Request(string Email, string Password);

    public record Response(Guid Id, string Name, string Email);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !VerifyHashedPassword(user.PasswordHash, request.Password))
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "The email or password provided is incorrect.",
                type: "https://tools.ietf.org/html/rfc7235#section-3.1"
            );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return TypedResults.Ok(new Response(user.Id, user.Name, user.Email));
    }

    private static async Task<User?> GetUserByEmailAsync(this ApplicationDbContext dbContext, string email,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    private static bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var parts = hashedPassword.Split('$');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var valueBytes = Encoding.UTF8.GetBytes(providedPassword);

        try
        {
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                valueBytes,
                salt,
                PasswordHasherParameters.Iterations,
                PasswordHasherParameters.HashAlgorithmName,
                expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        finally
        {
            Array.Clear(salt);
            Array.Clear(expectedHash);
            Array.Clear(valueBytes);
        }
    }
}