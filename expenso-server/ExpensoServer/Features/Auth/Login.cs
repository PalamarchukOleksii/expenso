using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Common.Abstractions;
using ExpensoServer.Common.Constants;
using ExpensoServer.Common.Extensions;
using ExpensoServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Auth;

public static class Login
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", HandleAsync)
                .WithRequestValidation<Request>()
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status401Unauthorized);
        }
    }

    public class Request
    {
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Required(ErrorMessage = "Email is required.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public required string Password { get; set; }
    };

    public record Response(Guid Id, string Email);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        Request request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null || !VerifyHashedPassword(user.PasswordHash, user.PasswordSalt, request.Password))
            return TypedResults.Problem(
                title: "Authentication Failed",
                detail: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return TypedResults.Ok(new Response(user.Id, user.Email));
    }

    private static bool VerifyHashedPassword(byte[] storedHash, byte[] storedSalt, string providedPassword)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(providedPassword);

        try
        {
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                storedSalt,
                PasswordHasherParameters.Iterations,
                PasswordHasherParameters.HashAlgorithmName,
                storedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(storedHash, actualHash);
        }
        finally
        {
            Array.Clear(passwordBytes);
        }
    }
}