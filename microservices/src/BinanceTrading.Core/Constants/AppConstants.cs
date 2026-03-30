namespace BinanceTrading.Core.Constants;

public static class AppConstants
{
    public const string TestnetUrl = "https://testnet.binancefuture.com";
    public const string TestnetFuturesUrl = "https://testnet.binancefuture.com";
    
    public const int DefaultTimeoutSeconds = 30;
    public const int MaxRetries = 3;
    
    public const int JwtExpiryHours = 24;
    public const string JwtIssuer = "BinanceTradingPlatform";
    public const string JwtAudience = "BinanceTradingUsers";
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Support = "Support";
}

public static class OrderStatusMessages
{
    public const string OrderPlaced = "Order placed successfully";
    public const string OrderFilled = "Order filled";
    public const string OrderCancelled = "Order cancelled";
    public const string OrderFailed = "Order execution failed";
    public const string InsufficientBalance = "Insufficient balance";
    public const string InvalidSymbol = "Invalid trading symbol";
    public const string ApiKeyNotConfigured = "API keys not configured";
}
