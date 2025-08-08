using System.Text.Json;
using System.Text.Json.Serialization;
using ExpensoServer.Common.Endpoints;

namespace ExpensoServer.Features.ExchangeRates;

public static class Get
{
    public class Endpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/", HandleAsync);
        }
    }

    public record Response(string FromCurrency, string ToCurrency, decimal Rate);

    private class CurrencyApiResponse
    {
        public string Date { get; set; } = null!;

        [JsonExtensionData] public Dictionary<string, JsonElement> Rates { get; set; } = null!;
    }

    private static async Task<IResult> HandleAsync(
        string fromCurrency,
        string toCurrency,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency) || string.IsNullOrWhiteSpace(toCurrency))
            return TypedResults.BadRequest();

        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return TypedResults.BadRequest();

        var from = fromCurrency.ToLowerInvariant();
        var to = toCurrency.ToLowerInvariant();
        var url = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{from}.json";

        var client = clientFactory.CreateClient();
        var json = await client.GetStringAsync(url, cancellationToken);

        var apiResponse = JsonSerializer.Deserialize<CurrencyApiResponse>(json);
        if (apiResponse is null || !apiResponse.Rates.TryGetValue(from, out var rateContainer))
            return TypedResults.BadRequest();

        if (!rateContainer.TryGetProperty(to, out var rateElement))
            return TypedResults.BadRequest();

        var rate = rateElement.GetDecimal();
        var response = new Response(fromCurrency, toCurrency, rate);
        return TypedResults.Ok(response);
    }
}