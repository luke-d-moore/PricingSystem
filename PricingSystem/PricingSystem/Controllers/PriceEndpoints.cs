using PricingSystem.Interfaces;

namespace PricingSystem.Controllers
{
    public static class PriceEndpoints
    {
        public static void MapEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("minimalapi/Price/");
            group.MapGet(nameof(GetAllPrices), GetAllPrices);
            group.MapGet(nameof(GetTickers), GetTickers);
            group.MapGet(nameof(GetPrice)+"/{ticker}", GetPrice);
        }

        public static async Task<IResult> GetPrice(string ticker, IPricingService pricingService)
        {
            try
            {
                var price = pricingService.GetCurrentPrice(ticker);
                var response = new GetPriceResponse(true, "Price Retrieved", new Dictionary<string, decimal>() { { ticker, price } });
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Price Retrieve Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound
                );
            }
        }

        public static async Task<IResult> GetAllPrices(IPricingService pricingService)
        {
            var prices = pricingService.GetPrices();
            var response = new GetPriceResponse(true, "Prices Retrieved", prices);
            return Results.Ok(response);
        }

        public static async Task<IResult> GetTickers(IPricingService pricingService)
        {
            var tickers = pricingService.GetTickers();
            var response = new GetTickersResponse(true, "Tickers Retrieved", tickers);
            return Results.Ok(response);
        }
    }
}
