using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BinanceTrading.ApiGateway.Controllers;

[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GatewayController> _logger;

    private const string UserServiceUrl = "http://user-service:5000";
    private const string TradingServiceUrl = "http://trading-service:5000";
    private const string AdminServiceUrl = "http://admin-service:5000";

    public GatewayController(IHttpClientFactory httpClientFactory, ILogger<GatewayController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Proxy to UserService - Register
    /// </summary>
    [HttpPost("auth/register")]
    public async Task<IActionResult> Register([FromBody] JsonElement request)
    {
        return await ProxyToService(UserServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to UserService - Login
    /// </summary>
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] JsonElement request)
    {
        return await ProxyToService(UserServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to UserService - Get Profile
    /// </summary>
    [HttpGet("auth/profile/{userId}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        return await ProxyToService(UserServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Place Order
    /// </summary>
    [HttpPost("trading/order/{userId}")]
    public async Task<IActionResult> PlaceOrder(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - Get Orders
    /// </summary>
    [HttpGet("trading/orders/{userId}")]
    public async Task<IActionResult> GetOrders(int userId)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Balance
    /// </summary>
    [HttpGet("trading/balance/{userId}")]
    public async Task<IActionResult> GetBalance(int userId)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Positions
    /// </summary>
    [HttpGet("trading/positions/{userId}")]
    public async Task<IActionResult> GetPositions(int userId)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - TWAP Strategy
    /// </summary>
    [HttpPost("trading/strategy/twap/{userId}")]
    public async Task<IActionResult> StartTWAP(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - OCO Strategy
    /// </summary>
    [HttpPost("trading/strategy/oco/{userId}")]
    public async Task<IActionResult> StartOCO(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method, request.GetRawText());
    }

    // ==================== /api/order/* routes (Frontend Compatibility) ====================

    /// <summary>
    /// Proxy to TradingService - Initialize Binance Client
    /// </summary>
    [HttpPost("order/initialize")]
    public async Task<IActionResult> InitializeClient([FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, "/api/order/initialize", HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - Place Order (api/order/{userId})
    /// </summary>
    [HttpPost("order/{userId}")]
    public async Task<IActionResult> PlaceOrderAlt(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/order/{userId}", HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - Get Orders (api/order/{userId})
    /// </summary>
    [HttpGet("order/{userId}")]
    public async Task<IActionResult> GetOrdersAlt(int userId, [FromQuery] string? symbol, [FromQuery] string? status)
    {
        var path = $"/api/order/{userId}";
        if (!string.IsNullOrEmpty(symbol) || !string.IsNullOrEmpty(status))
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            path += "?" + string.Join("&", queryParams);
        }
        return await ProxyToService(TradingServiceUrl, path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Specific Order
    /// </summary>
    [HttpGet("order/{userId}/{orderId}")]
    public async Task<IActionResult> GetOrder(int userId, long orderId, [FromQuery] string symbol)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/order/{userId}/{orderId}?symbol={symbol}", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Cancel Order
    /// </summary>
    [HttpDelete("order/{userId}/{orderId}")]
    public async Task<IActionResult> CancelOrder(int userId, long orderId, [FromQuery] string symbol)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/order/{userId}/{orderId}?symbol={symbol}", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Balance (api/order/{userId}/balance)
    /// </summary>
    [HttpGet("order/{userId}/balance")]
    public async Task<IActionResult> GetBalanceAlt(int userId)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/order/{userId}/balance", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Positions (api/order/{userId}/positions)
    /// </summary>
    [HttpGet("order/{userId}/positions")]
    public async Task<IActionResult> GetPositionsAlt(int userId, [FromQuery] string? symbol)
    {
        var path = $"/api/order/{userId}/positions";
        if (!string.IsNullOrEmpty(symbol))
        {
            path += $"?symbol={symbol}";
        }
        return await ProxyToService(TradingServiceUrl, path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Symbols
    /// </summary>
    [HttpGet("order/symbols")]
    public async Task<IActionResult> GetSymbols()
    {
        return await ProxyToService(TradingServiceUrl, "/api/order/symbols", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Get Price
    /// </summary>
    [HttpGet("order/price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/order/price/{symbol}", HttpContext.Request.Method);
    }

    // ==================== /api/strategy/* routes ====================

    /// <summary>
    /// Proxy to TradingService - TWAP Strategy
    /// </summary>
    [HttpPost("strategy/twap/{userId}")]
    public async Task<IActionResult> StartTWAPAlt(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/strategy/twap/{userId}", HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - OCO Strategy
    /// </summary>
    [HttpPost("strategy/oco/{userId}")]
    public async Task<IActionResult> StartOCOAlt(int userId, [FromBody] JsonElement request)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/strategy/oco/{userId}", HttpContext.Request.Method, request.GetRawText());
    }

    /// <summary>
    /// Proxy to TradingService - Get Active Strategies
    /// </summary>
    [HttpGet("strategy/{userId}")]
    public async Task<IActionResult> GetActiveStrategies(int userId)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/strategy/{userId}", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to TradingService - Stop TWAP Strategy
    /// </summary>
    [HttpDelete("strategy/twap/{userId}/{executionId}")]
    public async Task<IActionResult> StopTWAP(int userId, string executionId)
    {
        return await ProxyToService(TradingServiceUrl, $"/api/strategy/twap/{userId}/{executionId}", HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to AdminService - Get Stats
    /// </summary>
    [HttpGet("admin/stats")]
    public async Task<IActionResult> GetStats()
    {
        return await ProxyToService(AdminServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to AdminService - Get Users
    /// </summary>
    [HttpGet("admin/users")]
    public async Task<IActionResult> GetUsers()
    {
        return await ProxyToService(AdminServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to AdminService - Get Analytics
    /// </summary>
    [HttpGet("admin/analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        return await ProxyToService(AdminServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Proxy to AdminService - Get Settings
    /// </summary>
    [HttpGet("admin/settings")]
    public async Task<IActionResult> GetSettings()
    {
        return await ProxyToService(AdminServiceUrl, HttpContext.Request.Path, HttpContext.Request.Method);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ApiGateway",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Service discovery endpoint
    /// </summary>
    [HttpGet("services")]
    public IActionResult GetServices()
    {
        return Ok(new
        {
            services = new[]
            {
                new { name = "UserService", url = UserServiceUrl, status = "available" },
                new { name = "TradingService", url = TradingServiceUrl, status = "available" },
                new { name = "AdminService", url = AdminServiceUrl, status = "available" }
            }
        });
    }

    private async Task<IActionResult> ProxyToService(string baseUrl, string path, string method, string? body = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl}{path}";

            _logger.LogInformation("Proxying {Method} request to {Url}", method, url);

            HttpResponseMessage response;

            using (var requestMessage = new HttpRequestMessage(new HttpMethod(method), url))
            {
                if (!string.IsNullOrEmpty(body))
                {
                    requestMessage.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                }

                response = await client.SendAsync(requestMessage);
            }

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Response from {Url}: {StatusCode}", url, response.StatusCode);

            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {BaseUrl}", baseUrl);
            return StatusCode(503, new { message = "Service unavailable", error = ex.Message });
        }
    }
}
