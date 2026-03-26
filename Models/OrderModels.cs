// ============================================================
// Data Models
// These classes define the structure of data we use throughout the bot
// Think of them as blueprints for our data
// ============================================================

namespace Binance_bot_miniproject_papertrade.Models;

// ============================================================
// Order Side - Is this a BUY or SELL?
// ============================================================
public enum OrderSide
{
    BUY,   // Buy order
    SELL   // Sell order
}

// ============================================================
// Order Type - What kind of order?
// ============================================================
// MARKET: Buy/sell immediately at current market price
// LIMIT: Buy/sell only at specified price or better
// STOP: Trigger order when price reaches level
// TAKE_PROFIT: Like stop but for profit taking
public enum OrderType
{
    MARKET,
    LIMIT,
    STOP,
    TAKE_PROFIT,
    STOP_MARKET,
    TAKE_PROFIT_MARKET
}

// ============================================================
// Position Side - What direction is the position?
// ============================================================
// LONG: Expecting price to go up
// SHORT: Expecting price to go down
// BOTH: Hedge mode (can have both long and short)
public enum PositionSide
{
    LONG,
    SHORT,
    BOTH
}

// ============================================================
// Time In Force - How long should the order stay active?
// ============================================================
// GTC (Good Till Cancel): Order stays until filled or cancelled
// IOC (Immediate or Cancel): Fill what you can now, cancel rest
// FOK (Fill or Kill): Must fill entire order immediately or cancel
public enum TimeInForce
{
    GTC,
    IOC,
    FOK
}

// ============================================================
// Order Request - What we send to Binance when placing an order
// ============================================================
public class OrderRequest
{
    // Trading pair (e.g., "BTCUSDT")
    public string Symbol { get; set; } = string.Empty;
    
    // BUY or SELL
    public OrderSide Side { get; set; }
    
    // MARKET, LIMIT, STOP, etc.
    public OrderType Type { get; set; }
    
    // How much to buy/sell
    public decimal Quantity { get; set; }
    
    // For LIMIT orders - your target price
    public decimal? Price { get; set; }
    
    // GTC, IOC, or FOK
    public TimeInForce? TimeInForce { get; set; }
    
    // LONG, SHORT, or BOTH
    public PositionSide? PositionSide { get; set; }
    
    // For STOP orders - trigger price
    public decimal? StopPrice { get; set; }
    
    public decimal? WorkingType { get; set; }
}

// ============================================================
// Order Response - What Binance sends back after an order
// ============================================================
public class OrderResponse
{
    // Unique order ID from Binance
    public long OrderId { get; set; }
    
    public string Symbol { get; set; } = string.Empty;
    
    // Order status: NEW, FILLED, PARTIALLY_FILLED, CANCELLED, REJECTED
    public string Status { get; set; } = string.Empty;
    
    public string Side { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    
    public decimal Quantity { get; set; }
    
    public decimal? Price { get; set; }
    
    public decimal? StopPrice { get; set; }
    
    // Your custom order ID (optional)
    public string ClientOrderId { get; set; } = string.Empty;
    
    public long UpdateTime { get; set; }
    
    public bool IsWorking { get; set; }
}

// ============================================================
// OCO Order Request - For One-Cancels-the-Other orders
// ============================================================
// OCO places two orders: a limit order and a stop order
// When one executes, the other is automatically cancelled
public class OCOOrderRequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    
    // Target price for the limit order
    public decimal? Price { get; set; }
    
    // Trigger price for the stop order
    public decimal? StopPrice { get; set; }
    
    public PositionSide? PositionSide { get; set; }
    public string? StopLimitTimeInForce { get; set; }
    public decimal? StopLimitPrice { get; set; }
}

// ============================================================
// TWAP Request - For Time-Weighted Average Price strategy
// ============================================================
// TWAP splits a large order into smaller chunks
// Executes them at regular intervals to get average price
public class TWAPRequest
{
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    
    // Total amount to trade
    public decimal TotalQuantity { get; set; }
    
    // How many pieces to split into (default: 10)
    public int NumberOfTrades { get; set; } = 10;
    
    // Seconds between each trade (default: 60)
    public int IntervalSeconds { get; set; } = 60;
}

// ============================================================
// Account Info - Your account balance and margin
// ============================================================
public class AccountInfo
{
    // Total money in your wallet
    public decimal TotalWalletBalance { get; set; }
    
    // Profit/loss from open positions (not yet realized)
    public decimal TotalUnrealizedProfit { get; set; }
    
    // Total margin (including unrealized P/L)
    public decimal TotalMarginBalance { get; set; }
    
    // Money available to use for new orders
    public decimal AvailableBalance { get; set; }
    
    // Maximum you can withdraw
    public decimal MaxWithdrawAmount { get; set; }
}

// ============================================================
// Position - An open trading position
// ============================================================
public class Position
{
    public string Symbol { get; set; } = string.Empty;
    
    // How much you have (positive = long, negative = short)
    public decimal PositionAmount { get; set; }
    
    // Average price you entered at
    public decimal EntryPrice { get; set; }
    
    // Current profit/loss
    public decimal UnrealizedProfit { get; set; }
    
    public string PositionSide { get; set; } = string.Empty;
    
    // Your leverage (1x, 5x, 10x, etc.)
    public decimal Leverage { get; set; }
    
    // Margin used for this position (isolated mode)
    public decimal IsolatedMargin { get; set; }
    
    public string Status { get; set; } = string.Empty;
}

// ============================================================
// Trade Log - Record of trading activity
// ============================================================
public class TradeLog
{
    // When did this happen?
    public DateTime Timestamp { get; set; }
    
    // What action? (e.g., "Place Order", "Cancel Order")
    public string Action { get; set; } = string.Empty;
    
    // Which trading pair?
    public string Symbol { get; set; } = string.Empty;
    
    // Additional details
    public string Details { get; set; } = string.Empty;
    
    // Status: FILLED, CANCELLED, REJECTED, etc.
    public string Status { get; set; } = string.Empty;
}
