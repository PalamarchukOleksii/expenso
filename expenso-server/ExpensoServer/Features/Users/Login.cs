using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpensoServer.Abstractions;
using ExpensoServer.Data;
using ExpensoServer.Filters;
using ExpensoServer.Models;
using ExpensoServer.Shared;
using ExpensoServer.Shared.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ExpensoServer.Features.Users;

public static class Login
{
    public record Request(string Email, string Password);
    
    public record Response(Guid Id, string Name, string Email);

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost($"{EndpointTags.Users}/login", HandleAsync)
                .WithOpenApi()
                .WithTags(EndpointTags.Users)
                .AddEndpointFilter<ValidationFilter<Request>>()
                .Produces<Response>()
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesValidationProblem()
                .AllowAnonymous();
        }
    }

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        Request request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await GetUserByEmail(request.Email,dbContext, cancellationToken);
        if (user is null || !VerifyHashedPassword(user.PasswordHash, request.Password))
        {
            return TypedResults.Unauthorized();
        }

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
    
    private static async Task<User?> GetUserByEmail(string email, ApplicationDbContext dbContext,
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
                PasswordHasherParameters._hashAlgorithmName,
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
