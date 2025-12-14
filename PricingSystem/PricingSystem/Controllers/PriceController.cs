using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc.Routing;
using PricingSystem.Interfaces;

namespace PricingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceController : ControllerBase
    {
        private readonly ILogger<PriceController> _logger;
        private readonly IPricingService _pricingService;

        public PriceController(ILogger<PriceController> logger, IPricingService pricingService)
        {
            _logger = logger;
            _pricingService = pricingService;
        }

        // GET: api/<PriceController>
        [HttpGet(nameof(GetPrice) + "/{ticker}")]
        [SwaggerOperation(nameof(GetPrice))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        public IActionResult GetPrice(string ticker)
        {
            try
            {
                var price = _pricingService.GetCurrentPrice(ticker);
                var response = new GetPriceResponse(true, "Price Retrieved", new Dictionary<string, decimal>() { { ticker, price } });
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Price Retrieve Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound
                );
            }
        }

        // GET: api/<PriceController>
        [HttpGet]
        [Route(nameof(GetAllPrices))]
        [SwaggerOperation(nameof(GetAllPrices))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        public IActionResult GetAllPrices()
        {
            var tickers = _pricingService.GetPrices();
            if (tickers.Any())
            {
                var response = new GetPriceResponse(true, "Prices Retrieved", tickers);
                return Ok(response);
            }
            else
            {
                return Problem(
                    title: "Price Retrieve Failed",
                    detail: "No Prices Found",
                    statusCode: StatusCodes.Status404NotFound
                );
            }
        }

        // GET: api/<PriceController>
        [HttpGet]
        [Route(nameof(GetTickers))]
        [SwaggerOperation(nameof(GetTickers))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        public IActionResult GetTickers()
        {
            var tickers = _pricingService.GetTickers();
            var response = new GetTickersResponse(true, "Tickers Retrieved", tickers);
            return Ok(response);
        }
    }
}
