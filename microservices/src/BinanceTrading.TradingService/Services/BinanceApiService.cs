using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BinanceTrading.Core.Models;

namespace BinanceTrading.TradingService.Services;

public class BinanceApiService : IDisposable
{
    private readonly ILogger<BinanceApiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, (string ApiKey, string SecretKey)> _credentials = new();

    private const string TestnetBaseUrlV1 = "https://testnet.binancefuture.com/fapi/v1";
    private const string TestnetBaseUrlV2 = "https://testnet.binancefuture.com/fapi/v2";
    private const string TestnetSignUrl = "https://testnet.binancefuture.com";

    public BinanceApiService(ILogger<BinanceApiService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void InitializeClient(string userId, string apiKey, string secretKey)
    {
        _credentials[userId] = (apiKey, secretKey);
        _logger.LogInformation("Binance client initialized for user: {UserId}", userId);
    }

    public bool IsClientInitialized(string userId)
    {
        return _credentials.ContainsKey(userId);
    }

    public async Task<OrderResponse> PlaceOrderAsync(string userId, PlaceOrderRequest request)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation(
            "Placing order for user {UserId}: {Side} {Quantity} {Symbol} @ {Price}",
            userId, request.Side, request.Quantity, request.Symbol, request.Price);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "symbol", request.Symbol },
            { "side", request.Side == OrderSide.BUY ? "BUY" : "SELL" },
            { "type", ConvertOrderTypeToString(request.Type) },
            { "quantity", request.Quantity.ToString() },
            { "timestamp", timestamp.ToString() }
        };

        if (request.Price.HasValue)
        {
            queryParams["price"] = request.Price.Value.ToString();
            queryParams["timeInForce"] = "GTC";
        }

        if (request.StopPrice.HasValue)
        {
            queryParams["stopPrice"] = request.StopPrice.Value.ToString();
        }

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV1}/order?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to place order: {Response}", content);
            throw new Exception($"Failed to place order: {content}");
        }

        var orderData = JsonSerializer.Deserialize<BinanceOrderResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (orderData == null)
        {
            throw new Exception("Failed to parse order response");
        }

        _logger.LogInformation("Order placed successfully: {OrderId}", orderData.OrderId);

        return new OrderResponse
        {
            OrderId = orderData.OrderId,
            Symbol = orderData.Symbol,
            Side = orderData.Side,
            Type = orderData.Type,
            Quantity = decimal.Parse(orderData.OrigQty),
            Price = orderData.Price != null ? decimal.Parse(orderData.Price) : null,
            StopPrice = orderData.StopPrice != null ? decimal.Parse(orderData.StopPrice) : null,
            Status = orderData.Status
        };
    }

    public async Task<List<Order>> GetOrdersAsync(string userId, string? symbol = null, string? status = null)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation("Getting orders for user: {UserId}, Symbol: {Symbol}, Status: {Status}", userId, symbol, status);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "timestamp", timestamp.ToString() }
        };

        if (!string.IsNullOrEmpty(symbol))
        {
            queryParams["symbol"] = symbol;
        }

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV1}/allOrders?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get orders: {Response}", content);
            throw new Exception($"Failed to get orders: {content}");
        }

        var orders = JsonSerializer.Deserialize<List<BinanceOrderResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (orders == null)
        {
            return new List<Order>();
        }

        return orders.Select(o => new Order
        {
            Id = o.OrderId,
            Symbol = o.Symbol,
            Side = o.Side == "BUY" ? OrderSide.BUY : OrderSide.SELL,
            Type = ConvertStringToOrderType(o.Type),
            Quantity = decimal.Parse(o.OrigQty),
            Price = !string.IsNullOrEmpty(o.Price) ? decimal.Parse(o.Price) : null,
            StopPrice = !string.IsNullOrEmpty(o.StopPrice) ? decimal.Parse(o.StopPrice) : null,
            Status = ConvertStringToOrderStatus(o.Status),
            FilledQuantity = !string.IsNullOrEmpty(o.ExecutedQty) ? decimal.Parse(o.ExecutedQty) : null,
            BinanceOrderId = o.OrderId,
            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(o.Time).UtcDateTime,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(o.UpdateTime).UtcDateTime
        }).ToList();
    }

    public async Task<Order?> GetOrderAsync(string userId, long orderId, string symbol)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation("Getting order {OrderId} for user: {UserId}", orderId, userId);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "symbol", symbol },
            { "orderId", orderId.ToString() },
            { "timestamp", timestamp.ToString() }
        };

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV1}/order?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (content.Contains("Order does not exist"))
            {
                return null;
            }
            _logger.LogError("Failed to get order: {Response}", content);
            throw new Exception($"Failed to get order: {content}");
        }

        var o = JsonSerializer.Deserialize<BinanceOrderResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (o == null)
        {
            return null;
        }

        return new Order
        {
            Id = o.OrderId,
            Symbol = o.Symbol,
            Side = o.Side == "BUY" ? OrderSide.BUY : OrderSide.SELL,
            Type = ConvertStringToOrderType(o.Type),
            Quantity = decimal.Parse(o.OrigQty),
            Price = !string.IsNullOrEmpty(o.Price) ? decimal.Parse(o.Price) : null,
            StopPrice = !string.IsNullOrEmpty(o.StopPrice) ? decimal.Parse(o.StopPrice) : null,
            Status = ConvertStringToOrderStatus(o.Status),
            FilledQuantity = !string.IsNullOrEmpty(o.ExecutedQty) ? decimal.Parse(o.ExecutedQty) : null,
            BinanceOrderId = o.OrderId,
            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(o.Time).UtcDateTime,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(o.UpdateTime).UtcDateTime
        };
    }

    public async Task<bool> CancelOrderAsync(string userId, long orderId, string symbol)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation("Cancelling order {OrderId} for user: {UserId}", orderId, userId);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "symbol", symbol },
            { "orderId", orderId.ToString() },
            { "timestamp", timestamp.ToString() }
        };

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV1}/order?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to cancel order: {Response}", content);
            throw new Exception($"Failed to cancel order: {content}");
        }

        _logger.LogInformation("Order cancelled successfully: {OrderId}", orderId);
        return true;
    }

    public async Task<AccountBalance> GetAccountBalanceAsync(string userId)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation("Getting balance for user: {UserId}", userId);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "timestamp", timestamp.ToString() }
        };

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV2}/account?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get balance: {Response}", content);
            throw new Exception($"Failed to get balance: {content}");
        }

        var accountData = JsonSerializer.Deserialize<BinanceAccountResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (accountData == null)
        {
            throw new Exception("Failed to parse account response");
        }

        decimal totalWallet = 0;
        decimal availableBalance = 0;

        foreach (var balance in accountData.Balances)
        {
            var free = decimal.Parse(balance.Free);
            var locked = decimal.Parse(balance.Locked);
            if (free + locked > 0)
            {
                totalWallet += free;
                availableBalance += free;
            }
        }

        return new AccountBalance
        {
            UserId = int.TryParse(userId, out var uid) ? uid : 0,
            TotalWalletBalance = totalWallet,
            TotalUnrealizedPnl = 0,
            MarginBalance = totalWallet,
            AvailableBalance = availableBalance,
            MaxWithdrawAmount = availableBalance
        };
    }

    public async Task<List<Position>> GetPositionsAsync(string userId, string? symbol = null)
    {
        if (!_credentials.TryGetValue(userId, out var creds))
        {
            throw new InvalidOperationException($"Binance client not initialized for user: {userId}");
        }

        _logger.LogInformation("Getting positions for user: {UserId}, Symbol: {Symbol}", userId, symbol);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryParams = new Dictionary<string, string>
        {
            { "timestamp", timestamp.ToString() }
        };

        if (!string.IsNullOrEmpty(symbol))
        {
            queryParams["symbol"] = symbol;
        }

        var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var signature = ComputeHmacSha256(queryString, creds.SecretKey);

        var url = $"{TestnetBaseUrlV2}/positionRisk?{queryString}&signature={signature}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add("X-MBX-APIKEY", creds.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get positions: {Response}", content);
            throw new Exception($"Failed to get positions: {content}");
        }

        var positions = JsonSerializer.Deserialize<List<BinancePositionResponse>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (positions == null)
        {
            return new List<Position>();
        }

        return positions
            .Where(p => decimal.Parse(p.PositionAmt) != 0)
            .Select(p => new Position
            {
                Id = 0,
                Symbol = p.Symbol,
                Quantity = Math.Abs(decimal.Parse(p.PositionAmt)),
                EntryPrice = decimal.Parse(p.EntryPrice),
                CurrentPrice = decimal.Parse(p.MarkPrice),
                UnrealizedPnl = decimal.Parse(p.UnRealizedProfit),
                Leverage = decimal.Parse(p.Leverage),
                Side = decimal.Parse(p.PositionAmt) > 0 ? PositionSide.LONG : PositionSide.SHORT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();
    }

    public async Task<List<string>> GetAvailableSymbolsAsync()
    {
        var url = $"{TestnetBaseUrlV1}/exchangeInfo";

        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get symbols: {Response}", content);
            return new List<string> { "BTCUSDT", "ETHUSDT", "BNBUSDT" };
        }

        var exchangeInfo = JsonSerializer.Deserialize<BinanceExchangeInfo>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (exchangeInfo?.Symbols == null)
        {
            return new List<string> { "BTCUSDT", "ETHUSDT", "BNBUSDT" };
        }

        return exchangeInfo.Symbols
            .Where(s => s.Status == "TRADING")
            .Select(s => s.Symbol)
            .ToList();
    }

    public async Task<decimal?> GetCurrentPriceAsync(string symbol)
    {
        var url = $"{TestnetBaseUrlV1}/ticker/price?symbol={symbol}";

        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get price for {Symbol}: {Response}", symbol, content);
            return null;
        }

        var priceData = JsonSerializer.Deserialize<BinancePriceResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return priceData?.Price != null ? decimal.Parse(priceData.Price) : null;
    }

    private string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private string ConvertOrderTypeToString(OrderType type)
    {
        return type switch
        {
            OrderType.MARKET => "MARKET",
            OrderType.LIMIT => "LIMIT",
            OrderType.STOP => "STOP",
            OrderType.TAKE_PROFIT => "TAKE_PROFIT",
            _ => "LIMIT"
        };
    }

    private OrderType ConvertStringToOrderType(string type)
    {
        return type.ToUpper() switch
        {
            "MARKET" => OrderType.MARKET,
            "LIMIT" => OrderType.LIMIT,
            "STOP" => OrderType.STOP,
            "TAKE_PROFIT" => OrderType.TAKE_PROFIT,
            _ => OrderType.LIMIT
        };
    }

    private OrderStatus ConvertStringToOrderStatus(string status)
    {
        return status.ToUpper() switch
        {
            "NEW" => OrderStatus.NEW,
            "FILLED" => OrderStatus.FILLED,
            "PARTIALLY_FILLED" => OrderStatus.PARTIALLY_FILLED,
            "CANCELED" => OrderStatus.CANCELLED,
            "REJECTED" => OrderStatus.REJECTED,
            _ => OrderStatus.NEW
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _credentials.Clear();
    }
}

internal class BinanceOrderResponse
{
    public long OrderId { get; set; }
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Price { get; set; } = "";
    public string StopPrice { get; set; } = "";
    public string OrigQty { get; set; } = "";
    public string ExecutedQty { get; set; } = "";
    public long Time { get; set; }
    public long UpdateTime { get; set; }
}

internal class BinanceAccountResponse
{
    public List<BinanceBalance> Balances { get; set; } = new();
}

internal class BinanceBalance
{
    public string Asset { get; set; } = "";
    public string Free { get; set; } = "";
    public string Locked { get; set; } = "";
}

internal class BinancePositionResponse
{
    public string Symbol { get; set; } = "";
    public string PositionAmt { get; set; } = "";
    public string EntryPrice { get; set; } = "";
    public string MarkPrice { get; set; } = "";
    public string UnRealizedProfit { get; set; } = "";
    public string Leverage { get; set; } = "";
}

internal class BinanceExchangeInfo
{
    public List<BinanceSymbol> Symbols { get; set; } = new();
}

internal class BinanceSymbol
{
    public string Symbol { get; set; } = "";
    public string Status { get; set; } = "";
}

internal class BinancePriceResponse
{
    public string Symbol { get; set; } = "";
    public string Price { get; set; } = "";
}
