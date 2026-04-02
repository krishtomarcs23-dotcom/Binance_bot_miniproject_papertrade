using Microsoft.AspNetCore.Mvc;
using BinanceTrading.Core.Models;
using BinanceTrading.TradingService.Services;

namespace BinanceTrading.TradingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> _logger;
    private readonly BinanceApiService _binanceService;

    public OrderController(ILogger<OrderController> logger, BinanceApiService binanceService)
    {
        _logger = logger;
        _binanceService = binanceService;
    }

    [HttpPost("initialize")]
    public ActionResult InitializeClient([FromBody] InitializeClientRequest request)
    {
        try
        {
            _binanceService.InitializeClient(request.UserId, request.ApiKey, request.SecretKey);
            return Ok(new { message = "Binance client initialized successfully", userId = request.UserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Binance client");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{userId}")]
    public async Task<ActionResult<OrderResponse>> PlaceOrder(int userId, [FromBody] PlaceOrderRequest request)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            var response = await _binanceService.PlaceOrderAsync(userId.ToString(), request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<Order>>> GetOrders(int userId, [FromQuery] string? symbol = null, [FromQuery] string? status = null)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            var orders = await _binanceService.GetOrdersAsync(userId.ToString(), symbol, status);
            foreach (var order in orders)
            {
                order.UserId = userId;
            }
            return Ok(orders);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{userId}/{orderId}")]
    public async Task<ActionResult<Order>> GetOrder(int userId, long orderId, [FromQuery] string symbol)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            var order = await _binanceService.GetOrderAsync(userId.ToString(), orderId, symbol);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }
            order.UserId = userId;
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order: {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{userId}/{orderId}")]
    public async Task<ActionResult> CancelOrder(int userId, long orderId, [FromQuery] string symbol)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            await _binanceService.CancelOrderAsync(userId.ToString(), orderId, symbol);
            return Ok(new { message = "Order cancelled successfully", orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order: {OrderId}", orderId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{userId}/balance")]
    public async Task<ActionResult<AccountBalance>> GetBalance(int userId)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            var balance = await _binanceService.GetAccountBalanceAsync(userId.ToString());
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for user: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{userId}/positions")]
    public async Task<ActionResult<List<Position>>> GetPositions(int userId, [FromQuery] string? symbol = null)
    {
        try
        {
            if (!_binanceService.IsClientInitialized(userId.ToString()))
            {
                return BadRequest(new { message = "Binance client not initialized. Please call /api/order/initialize first." });
            }

            var positions = await _binanceService.GetPositionsAsync(userId.ToString(), symbol);
            foreach (var position in positions)
            {
                position.UserId = userId;
            }
            return Ok(positions);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Binance client not initialized for user: {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get positions for user: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("symbols")]
    public async Task<ActionResult<List<string>>> GetSymbols()
    {
        try
        {
            var symbols = await _binanceService.GetAvailableSymbolsAsync();
            return Ok(symbols);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get symbols");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("price/{symbol}")]
    public async Task<ActionResult<object>> GetPrice(string symbol)
    {
        try
        {
            var price = await _binanceService.GetCurrentPriceAsync(symbol);
            if (price == null)
            {
                return NotFound(new { message = "Symbol not found" });
            }
            return Ok(new { symbol, price });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get price for {Symbol}", symbol);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class InitializeClientRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
