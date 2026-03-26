// ============================================================
// Binance API Service
// This file handles all communication with Binance Futures API
// ============================================================

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Binance_bot_miniproject_papertrade.Configuration;
using Binance_bot_miniproject_papertrade.Models;
using Binance_bot_miniproject_papertrade.Utils;

namespace Binance_bot_miniproject_papertrade.Services;

// This class handles all HTTP requests to Binance API
public class BinanceApiService
{
    // HTTP client to send requests
    private readonly HttpClient _httpClient;
    private readonly LoggerService _logger;
    private readonly AppConfig _config;
    private readonly string _baseUrl;

    // Constructor - set up the HTTP client with testnet URL
    public BinanceApiService(LoggerService logger)
    {
        _logger = logger;
        _config = ConfigLoader.Load();
        _baseUrl = _config.TestnetFuturesUrl;
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    // ============================================================
    // Generate Signature for API Security
    // ============================================================
    // Binance requires a signature to verify API requests
    // This creates a HMAC-SHA256 hash of the request parameters
    // using your secret key
    private string GenerateSignature(string queryString)
    {
        // Create HMAC-SHA256 hash with secret key
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.SecretKey));
        // Hash the query string
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        // Convert to lowercase hex string
        return Convert.ToHexString(hash).ToLower();
    }

    // ============================================================
    // Send Request to Binance API
    // ============================================================
    // This is the main method that sends any request to Binance
    // endpoint: API path (e.g., "/fapi/v1/order")
    // method: HTTP method (GET, POST, DELETE)
    // parameters: query parameters
    // signed: whether request needs authentication
    private async Task<string> SendRequestAsync(string endpoint, string method, Dictionary<string, string>? parameters = null, bool signed = false)
    {
        var queryString = string.Empty;
        
        // If we have parameters, convert them to query string
        if (parameters != null && parameters.Count > 0)
        {
            // Join parameters like: "symbol=BTCUSDT&side=BUY&quantity=0.01"
            queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            
            // For signed requests, add timestamp and signature
            if (signed)
            {
                queryString += $"&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var signature = GenerateSignature(queryString);
                queryString += $"&signature={signature}";
            }
        }
        else if (signed)
        {
            // Even without parameters, signed requests need timestamp
            queryString = $"timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var signature = GenerateSignature(queryString);
            queryString += $"&signature={signature}";
        }

        // Build the full URL
        var url = $"{endpoint}";
        if (!string.IsNullOrEmpty(queryString))
        {
            url += "?" + queryString;
        }

        // Create HTTP request
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        
        // Add API key header for signed requests
        if (signed && !string.IsNullOrEmpty(_config.ApiKey))
        {
            request.Headers.Add("X-MBX-APIKEY", _config.ApiKey);
        }

        // Send request and get response
        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Check if request was successful
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"API Error: {response.StatusCode} - {content}");
            throw new Exception($"API Error: {response.StatusCode} - {content}");
        }

        return content;
    }

    // ============================================================
    // Place Order - Main method for placing any order
    // ============================================================
    public async Task<OrderResponse> PlaceOrder(OrderRequest request)
    {
        // Prepare order parameters
        var parameters = new Dictionary<string, string>
        {
            { "symbol", request.Symbol.ToUpper() },
            { "side", request.Side.ToString() },
            { "type", request.Type.ToString() },
            { "quantity", request.Quantity.ToString() }
        };

        // Add price for limit orders
        if (request.Price.HasValue)
        {
            parameters["price"] = request.Price.Value.ToString();
            parameters["timeInForce"] = request.TimeInForce?.ToString() ?? "GTC";
        }

        // Add stop price for stop orders
        if (request.StopPrice.HasValue)
        {
            parameters["stopPrice"] = request.StopPrice.Value.ToString();
        }

        // Add position side for hedge mode
        if (request.PositionSide.HasValue)
        {
            parameters["positionSide"] = request.PositionSide.Value.ToString();
        }

        parameters["newOrderRespType"] = "RESULT";

        _logger.LogInfo($"Placing {request.Type} order: {request.Symbol} {request.Side} {request.Quantity}");
        
        // Send order to Binance
        var response = await SendRequestAsync("/fapi/v1/order", "POST", parameters, true);
        
        // Parse JSON response
        var result = JsonSerializer.Deserialize<OrderResponse>(response, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Log the order
        _logger.LogOrder(new TradeLog
        {
            Timestamp = DateTime.Now,
            Action = $"Place {request.Type} Order",
            Symbol = request.Symbol,
            Details = $"Side: {request.Side}, Qty: {request.Quantity}, Price: {request.Price}",
            Status = result?.Status ?? "UNKNOWN"
        });

        return result ?? throw new Exception("Failed to parse order response");
    }

    // Place a limit order (buy/sell at specific price)
    public async Task<OrderResponse> PlaceLimitOrder(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        var request = new OrderRequest
        {
            Symbol = symbol.ToUpper(),
            Side = side,
            Type = OrderType.LIMIT,
            Quantity = quantity,
            Price = price,
            TimeInForce = TimeInForce.GTC
        };

        return await PlaceOrder(request);
    }

    // Place a market order (buy/sell immediately at current price)
    public async Task<OrderResponse> PlaceMarketOrder(string symbol, OrderSide side, decimal quantity)
    {
        var request = new OrderRequest
        {
            Symbol = symbol.ToUpper(),
            Side = side,
            Type = OrderType.MARKET,
            Quantity = quantity
        };

        return await PlaceOrder(request);
    }

    // Get status of a specific order
    public async Task<OrderResponse> GetOrderStatus(string symbol, long orderId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "symbol", symbol.ToUpper() },
            { "orderId", orderId.ToString() }
        };

        var response = await SendRequestAsync("/fapi/v1/order", "GET", parameters, true);
        
        return JsonSerializer.Deserialize<OrderResponse>(response, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        }) ?? throw new Exception("Failed to parse order response");
    }

    // Cancel an existing order
    public async Task<bool> CancelOrder(string symbol, long orderId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "symbol", symbol.ToUpper() },
            { "orderId", orderId.ToString() }
        };

        _logger.LogInfo($"Cancelling order {orderId} on {symbol}");
        
        await SendRequestAsync("/fapi/v1/order", "DELETE", parameters, true);
        
        _logger.LogOrder(new TradeLog
        {
            Timestamp = DateTime.Now,
            Action = "Cancel Order",
            Symbol = symbol,
            Details = $"OrderId: {orderId}",
            Status = "CANCELLED"
        });

        return true;
    }

    // Get all open orders (optionally for specific symbol)
    public async Task<List<OrderResponse>> GetOpenOrders(string? symbol = null)
    {
        var parameters = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(symbol))
        {
            parameters["symbol"] = symbol.ToUpper();
        }

        var response = await SendRequestAsync("/fapi/v1/openOrders", "GET", parameters, true);
        
        return JsonSerializer.Deserialize<List<OrderResponse>>(response, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        }) ?? new List<OrderResponse>();
    }

    // Get account balance and margin info
    public async Task<AccountInfo> GetAccountInfo()
    {
        var response = await SendRequestAsync("/fapi/v2/account", "GET", null, true);
        
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        
        return new AccountInfo
        {
            TotalWalletBalance = decimal.Parse(root.GetProperty("totalWalletBalance").GetString() ?? "0"),
            TotalUnrealizedProfit = decimal.Parse(root.GetProperty("totalUnrealizedProfit").GetString() ?? "0"),
            TotalMarginBalance = decimal.Parse(root.GetProperty("totalMarginBalance").GetString() ?? "0"),
            AvailableBalance = decimal.Parse(root.GetProperty("availableBalance").GetString() ?? "0"),
            MaxWithdrawAmount = decimal.Parse(root.GetProperty("maxWithdrawAmount").GetString() ?? "0")
        };
    }

    // Get all open positions (optionally for specific symbol)
    public async Task<List<Position>> GetPositions(string? symbol = null)
    {
        var parameters = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(symbol))
        {
            parameters["symbol"] = symbol.ToUpper();
        }

        var response = await SendRequestAsync("/fapi/v2/positionRisk", "GET", parameters, true);
        
        using var doc = JsonDocument.Parse(response);
        var positions = new List<Position>();
        
        // Parse each position from response
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var positionAmt = decimal.Parse(element.GetProperty("positionAmt").GetString() ?? "0");
            // Only include positions that have amount (not zero)
            if (positionAmt != 0)
            {
                positions.Add(new Position
                {
                    Symbol = element.GetProperty("symbol").GetString() ?? "",
                    PositionAmount = positionAmt,
                    EntryPrice = decimal.Parse(element.GetProperty("entryPrice").GetString() ?? "0"),
                    UnrealizedProfit = decimal.Parse(element.GetProperty("unrealizedProfit").GetString() ?? "0"),
                    PositionSide = element.GetProperty("positionSide").GetString() ?? "",
                    Leverage = decimal.Parse(element.GetProperty("leverage").GetString() ?? "0"),
                    IsolatedMargin = decimal.Parse(element.GetProperty("isolatedMargin").GetString() ?? "0"),
                    Status = element.GetProperty("updateTime").GetInt64().ToString()
                });
            }
        }
        
        return positions;
    }

    // Get current price for a symbol
    public async Task<decimal> GetCurrentPrice(string symbol)
    {
        var response = await SendRequestAsync("/fapi/v1/ticker/price", "GET", 
            new Dictionary<string, string> { { "symbol", symbol.ToUpper() } });
        
        using var doc = JsonDocument.Parse(response);
        return decimal.Parse(doc.RootElement.GetProperty("price").GetString() ?? "0");
    }

    // Get all prices (for all trading pairs)
    public async Task<Dictionary<string, decimal>> GetAllPrices()
    {
        var response = await SendRequestAsync("/fapi/v1/ticker/price", "GET");
        
        var prices = new Dictionary<string, decimal>();
        using var doc = JsonDocument.Parse(response);
        
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var symbol = element.GetProperty("symbol").GetString() ?? "";
            var price = decimal.Parse(element.GetProperty("price").GetString() ?? "0");
            prices[symbol] = price;
        }
        
        return prices;
    }

    // Change leverage for a symbol
    // Leverage: 1x = no leverage, 10x = 10x profit/loss
    public async Task ChangeLeverage(string symbol, int leverage)
    {
        var parameters = new Dictionary<string, string>
        {
            { "symbol", symbol.ToUpper() },
            { "leverage", leverage.ToString() }
        };

        _logger.LogInfo($"Setting leverage to {leverage}x for {symbol}");
        await SendRequestAsync("/fapi/v1/leverage", "POST", parameters, true);
    }

    // Check if API keys are configured
    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_config.ApiKey) && !string.IsNullOrEmpty(_config.SecretKey);
    }
}
