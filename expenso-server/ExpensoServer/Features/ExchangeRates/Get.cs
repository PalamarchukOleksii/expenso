using System.Text.Json;
using System.Text.Json.Serialization;
using ExpensoServer.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpensoServer.Features.ExchangeRates;

public static class Get
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync)
                .Produces<Response>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
    }

    public record Response(string FromCurrency, string ToCurrency, decimal Rate);

    private class CurrencyApiResponse
    {
        public string Date { get; set; } = null!;
        [JsonExtensionData] public Dictionary<string, JsonElement> Rates { get; set; } = null!;
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> HandleAsync(
        string fromCurrency,
        string toCurrency,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            return TypedResults.Problem(
                title: "Invalid input",
                detail: "FromCurrency and ToCurrency query parameters must be provided.",
                statusCode: StatusCodes.Status400BadRequest);

        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return TypedResults.Problem(
                title: "Invalid input",
                detail: "FromCurrency and ToCurrency cannot be the same.",
                statusCode: StatusCodes.Status400BadRequest);

        var from = fromCurrency.ToLowerInvariant();
        var to = toCurrency.ToLowerInvariant();

        var url = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{from}.json";

        var client = clientFactory.CreateClient();

        string json;
        try
        {
            json = await client.GetStringAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return TypedResults.Problem(
                title: "External API error",
                detail: $"Could not retrieve exchange rates: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var apiResponse = JsonSerializer.Deserialize<CurrencyApiResponse>(json);
        if (apiResponse?.Rates == null)
            return TypedResults.Problem(
                title: "External API error",
                detail: "Received invalid data from exchange rates API.",
                statusCode: StatusCodes.Status500InternalServerError);

        if (!apiResponse.Rates.TryGetValue(to, out var rateElement))
            return TypedResults.Problem(
                title: "Currency not supported",
                detail: $"The currency '{toCurrency}' is not supported.",
                statusCode: StatusCodes.Status400BadRequest);

        if (!rateElement.TryGetDecimal(out var rate))
            return TypedResults.Problem(
                title: "External API error",
                detail: "Exchange rate data is malformed.",
                statusCode: StatusCodes.Status500InternalServerError);

        var response = new Response(fromCurrency, toCurrency, rate);
        return TypedResults.Ok(response);
    }
}