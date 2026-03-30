namespace BinanceTrading.Core.Models;

public enum OrderSide { BUY, SELL }
public enum OrderType { MARKET, LIMIT, STOP, TAKE_PROFIT }
public enum PositionSide { LONG, SHORT, BOTH }
public enum TimeInForce { GTC, IOC, FOK }
public enum OrderStatus { NEW, FILLED, PARTIALLY_FILLED, CANCELLED, REJECTED }

public class Order
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }
    public TimeInForce? TimeInForce { get; set; }
    public OrderStatus Status { get; set; }
    public decimal? FilledQuantity { get; set; }
    public decimal? AveragePrice { get; set; }
    public long? BinanceOrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Position
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal Leverage { get; set; }
    public PositionSide Side { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AccountBalance
{
    public int UserId { get; set; }
    public decimal TotalWalletBalance { get; set; }
    public decimal TotalUnrealizedPnl { get; set; }
    public decimal MarginBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal MaxWithdrawAmount { get; set; }
}

public class TWAPRequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal TotalQuantity { get; set; }
    public int NumberOfTrades { get; set; } = 10;
    public int IntervalSeconds { get; set; } = 60;
}

public class OCORequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal LimitPrice { get; set; }
    public decimal StopPrice { get; set; }
}

public class TWAPExecution
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal TotalQuantity { get; set; }
    public int NumberOfTrades { get; set; }
    public int IntervalSeconds { get; set; }
    public int CompletedTrades { get; set; }
    public decimal FilledQuantity { get; set; }
    public bool IsRunning { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PlaceOrderRequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }
}

public class OrderResponse
{
    public long OrderId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? StopPrice { get; set; }
}
