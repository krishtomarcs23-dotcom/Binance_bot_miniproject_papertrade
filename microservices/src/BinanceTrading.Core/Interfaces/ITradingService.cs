using BinanceTrading.Core.Models;

namespace BinanceTrading.Core.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> PlaceMarketOrderAsync(int userId, PlaceOrderRequest request);
    Task<OrderResponse> PlaceLimitOrderAsync(int userId, PlaceOrderRequest request);
    Task<Order?> GetOrderAsync(int userId, long orderId);
    Task<List<Order>> GetOpenOrdersAsync(int userId, string? symbol = null);
    Task<List<Order>> GetOrderHistoryAsync(int userId, string? symbol = null, int limit = 50);
    Task<bool> CancelOrderAsync(int userId, long orderId);
    Task<OrderResponse> ExecuteTWAPAsync(int userId, TWAPRequest request);
    Task<OrderResponse> ExecuteOCOAsync(int userId, OCORequest request);
    void StopTWAP(int userId);
}

public interface IAccountService
{
    Task<AccountBalance> GetAccountBalanceAsync(int userId);
    Task<List<Position>> GetPositionsAsync(int userId, string? symbol = null);
    Task<bool> SetLeverageAsync(int userId, string symbol, int leverage);
}

public interface IBinanceApiClient
{
    Task<OrderResponse> PlaceOrderAsync(string apiKey, string secretKey, OrderRequest request);
    Task<OrderResponse> GetOrderAsync(string apiKey, string secretKey, string symbol, long orderId);
    Task<bool> CancelOrderAsync(string apiKey, string secretKey, string symbol, long orderId);
    Task<List<OrderResponse>> GetOpenOrdersAsync(string apiKey, string secretKey, string? symbol = null);
    Task<AccountBalance> GetAccountInfoAsync(string apiKey, string secretKey);
    Task<List<Position>> GetPositionsAsync(string apiKey, string secretKey, string? symbol = null);
    Task<decimal> GetPriceAsync(string symbol);
    Task<bool> SetLeverageAsync(string apiKey, string secretKey, string symbol, int leverage);
}

public class OrderRequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }
    public TimeInForce? TimeInForce { get; set; }
}
