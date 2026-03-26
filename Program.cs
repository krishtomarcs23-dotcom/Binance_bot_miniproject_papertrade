// ============================================================
// Binance Futures CLI Trading Bot
// Main Program - Entry point of the application
// 
// This is a paper trading bot that lets you practice
// cryptocurrency futures trading without risking real money
// ============================================================

using Binance_bot_miniproject_papertrade.Models;
using Binance_bot_miniproject_papertrade.Services;
using Binance_bot_miniproject_papertrade.Utils;

namespace Binance_bot_miniproject_papertrade;

// Main program class
class Program
{
    // Global service instances - these handle different parts of the bot
    private static LoggerService? _logger;
    private static BinanceApiService? _apiService;
    private static ValidationService? _validationService;
    private static OrderService? _orderService;
    private static StrategyService? _strategyService;

    // Flag to track if TWAP is running
    private static bool _isTWAPRunning = false;

    // ============================================================
    // Main Method - Program starts here!
    // ============================================================
    static async Task Main(string[] args)
    {
        // Display welcome banner
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Binance Futures CLI Trading Bot (Paper Trading)       ║");
        Console.WriteLine("║                    Testnet Mode                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Step 1: Initialize all services
        InitializeServices();

        // Step 2: Check if API keys are configured
        // If not, ask user to enter them
        if (!_apiService!.IsConfigured())
        {
            Console.WriteLine("API keys not configured. Please set up your keys first.");
            await SetupApiKeys();
        }

        // Step 3: If command line arguments provided, run those
        // Otherwise show interactive menu
        if (args.Length > 0)
        {
            await ProcessCommand(args);
            return;
        }

        // Show interactive menu
        await ShowInteractiveMenu();
    }

    // ============================================================
    // Initialize Services
    // Sets up all the services our bot needs
    // ============================================================
    private static void InitializeServices()
    {
        // Create services in order (dependencies)
        _logger = new LoggerService();                                    // Logging
        _apiService = new BinanceApiService(_logger);                    // API communication
        _validationService = new ValidationService(_logger);            // Input validation
        _orderService = new OrderService(_apiService, _validationService, _logger);  // Order handling
        _strategyService = new StrategyService(_orderService, _validationService, _logger, _apiService);  // Strategies

        _logger.LogInfo("Binance Futures Bot initialized");
    }

    // ============================================================
    // Setup API Keys
    // Prompts user to enter their Binance testnet API keys
    // ============================================================
    private static async Task SetupApiKeys()
    {
        Console.Write("\nEnter Binance Testnet API Key: ");
        var apiKey = Console.ReadLine() ?? "";

        Console.Write("Enter Binance Testnet Secret Key: ");
        var secretKey = Console.ReadLine() ?? "";

        if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(secretKey))
        {
            // Save keys to config file
            ConfigLoader.UpdateApiKeys(apiKey, secretKey);
            Console.WriteLine("API keys saved successfully!");
            // Re-initialize services with new keys
            InitializeServices();
        }
        else
        {
            Console.WriteLine("Invalid API keys. Bot may not work properly.");
        }
    }

    // ============================================================
    // Process Command
    // Handles command-line arguments
    // Example: dotnet run -- market BTCUSDT BUY 0.01
    // ============================================================
    private static async Task ProcessCommand(string[] args)
    {
        // Get the command (first argument)
        var command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "market":
                case "m":
                    if (args.Length >= 4)
                    {
                        var symbol = args[1];
                        var side = ParseSide(args[2]);
                        var quantity = decimal.Parse(args[3]);
                        await PlaceMarketOrder(symbol, side, quantity);
                    }
                    else Console.WriteLine("Usage: market <SYMBOL> <BUY|SELL> <QUANTITY>");
                    break;

                case "limit":
                case "l":
                    if (args.Length >= 5)
                    {
                        var symbol = args[1];
                        var side = ParseSide(args[2]);
                        var quantity = decimal.Parse(args[3]);
                        var price = decimal.Parse(args[4]);
                        await PlaceLimitOrder(symbol, side, quantity, price);
                    }
                    else Console.WriteLine("Usage: limit <SYMBOL> <BUY|SELL> <QUANTITY> <PRICE>");
                    break;

                case "twap":
                    await ProcessTWAP(args);
                    break;

                case "oco":
                    await ProcessOCO(args);
                    break;

                case "account":
                case "acc":
                    await ShowAccountInfo();
                    break;

                case "positions":
                case "pos":
                    await ShowPositions(args.Length > 1 ? args[1] : null);
                    break;

                case "price":
                case "p":
                    if (args.Length >= 2)
                        await ShowPrice(args[1]);
                    else Console.WriteLine("Usage: price <SYMBOL>");
                    break;

                case "leverage":
                    if (args.Length >= 3)
                    {
                        var symbol = args[1];
                        var leverage = int.Parse(args[2]);
                        await SetLeverage(symbol, leverage);
                    }
                    else Console.WriteLine("Usage: leverage <SYMBOL> <LEVERAGE>");
                    break;

                case "cancel":
                    if (args.Length >= 3)
                    {
                        var symbol = args[1];
                        var orderId = long.Parse(args[2]);
                        await CancelOrder(symbol, orderId);
                    }
                    else Console.WriteLine("Usage: cancel <SYMBOL> <ORDER_ID>");
                    break;

                case "openorders":
                case "open":
                    await ShowOpenOrders(args.Length > 1 ? args[1] : null);
                    break;

                case "logs":
                case "history":
                    ShowLogs(args.Length > 1 ? int.Parse(args[1]) : 20);
                    break;

                case "help":
                case "h":
                case "?":
                    ShowHelp();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            _logger?.LogError($"Command failed: {command} - {ex.Message}");
        }
    }

    private static async Task ShowInteractiveMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│           MAIN MENU                     │");
            Console.WriteLine("├─────────────────────────────────────────┤");
            Console.WriteLine("│  1. Place Market Order                 │");
            Console.WriteLine("│  2. Place Limit Order                  │");
            Console.WriteLine("│  3. Execute TWAP Strategy              │");
            Console.WriteLine("│  4. Execute OCO Order                  │");
            Console.WriteLine("│  5. View Account Info                  │");
            Console.WriteLine("│  6. View Positions                     │");
            Console.WriteLine("│  7. Check Price                        │");
            Console.WriteLine("│  8. Set Leverage                       │");
            Console.WriteLine("│  9. Cancel Order                       │");
            Console.WriteLine("│ 10. View Open Orders                   │");
            Console.WriteLine("│ 11. View Trade Logs                    │");
            Console.WriteLine("│ 12. Configure API Keys                 │");
            Console.WriteLine("│  0. Exit                               │");
            Console.WriteLine("└─────────────────────────────────────────┘");
            Console.Write("Select option: ");

            var input = Console.ReadLine();
            Console.WriteLine();

            if (input == "0") break;

            await ProcessMenuOption(input);
        }
    }

    private static async Task ProcessMenuOption(string? option)
    {
        try
        {
            switch (option)
            {
                case "1":
                    var (mSymbol, mSide, mQty) = GetOrderInput();
                    await PlaceMarketOrder(mSymbol, mSide, mQty);
                    break;

                case "2":
                    var (lSymbol, lSide, lQty, lPrice) = GetLimitOrderInput();
                    await PlaceLimitOrder(lSymbol, lSide, lQty, lPrice);
                    break;

                case "3":
                    await GetAndExecuteTWAP();
                    break;

                case "4":
                    await GetAndExecuteOCO();
                    break;

                case "5":
                    await ShowAccountInfo();
                    break;

                case "6":
                    Console.Write("Symbol (or press Enter for all): ");
                    var posSymbol = Console.ReadLine();
                    await ShowPositions(posSymbol);
                    break;

                case "7":
                    Console.Write("Symbol: ");
                    var priceSymbol = Console.ReadLine();
                    if (!string.IsNullOrEmpty(priceSymbol))
                        await ShowPrice(priceSymbol);
                    break;

                case "8":
                    Console.Write("Symbol: ");
                    var levSymbol = Console.ReadLine();
                    Console.Write("Leverage (1-125): ");
                    var leverage = int.Parse(Console.ReadLine() ?? "10");
                    if (!string.IsNullOrEmpty(levSymbol))
                        await SetLeverage(levSymbol, leverage);
                    break;

                case "9":
                    Console.Write("Symbol: ");
                    var cSymbol = Console.ReadLine();
                    Console.Write("Order ID: ");
                    var orderId = long.Parse(Console.ReadLine() ?? "0");
                    if (!string.IsNullOrEmpty(cSymbol))
                        await CancelOrder(cSymbol, orderId);
                    break;

                case "10":
                    Console.Write("Symbol (or press Enter for all): ");
                    var openSymbol = Console.ReadLine();
                    await ShowOpenOrders(openSymbol);
                    break;

                case "11":
                    ShowLogs(20);
                    break;

                case "12":
                    await SetupApiKeys();
                    break;

                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static (string symbol, OrderSide side, decimal quantity) GetOrderInput()
    {
        Console.Write("Symbol (e.g., BTCUSDT): ");
        var symbol = Console.ReadLine() ?? "";

        Console.Write("Side (BUY/SELL): ");
        var side = ParseSide(Console.ReadLine() ?? "BUY");

        Console.Write("Quantity: ");
        var quantity = decimal.Parse(Console.ReadLine() ?? "0");

        return (symbol, side, quantity);
    }

    private static (string symbol, OrderSide side, decimal quantity, decimal price) GetLimitOrderInput()
    {
        var (symbol, side, quantity) = GetOrderInput();
        Console.Write("Price: ");
        var price = decimal.Parse(Console.ReadLine() ?? "0");
        return (symbol, side, quantity, price);
    }

    private static async Task GetAndExecuteTWAP()
    {
        Console.Write("Symbol: ");
        var symbol = Console.ReadLine() ?? "";

        Console.Write("Side (BUY/SELL): ");
        var side = ParseSide(Console.ReadLine() ?? "BUY");

        Console.Write("Total Quantity: ");
        var totalQty = decimal.Parse(Console.ReadLine() ?? "0");

        Console.Write("Number of Trades (default 10): ");
        var numTrades = int.Parse(Console.ReadLine() ?? "10");

        Console.Write("Interval in Seconds (default 60): ");
        var interval = int.Parse(Console.ReadLine() ?? "60");

        _isTWAPRunning = true;

        var task = _strategyService!.ExecuteTWAP(new TWAPRequest
        {
            Symbol = symbol,
            Side = side,
            TotalQuantity = totalQty,
            NumberOfTrades = numTrades,
            IntervalSeconds = interval
        });

        Console.WriteLine("TWAP running... Press 'x' to stop.");

        while (_isTWAPRunning && !task.IsCompleted)
        {
            if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.X)
            {
                _strategyService.StopTWAP();
                break;
            }
            await Task.Delay(100);
        }
    }

    private static async Task GetAndExecuteOCO()
    {
        Console.Write("Symbol: ");
        var symbol = Console.ReadLine() ?? "";

        Console.Write("Side (BUY/SELL): ");
        var side = ParseSide(Console.ReadLine() ?? "BUY");

        Console.Write("Quantity: ");
        var quantity = decimal.Parse(Console.ReadLine() ?? "0");

        Console.Write("Limit Price: ");
        var limitPrice = decimal.Parse(Console.ReadLine() ?? "0");

        Console.Write("Stop Price: ");
        var stopPrice = decimal.Parse(Console.ReadLine() ?? "0");

        await _strategyService!.ExecuteOCO(new OCOOrderRequest
        {
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = limitPrice,
            StopPrice = stopPrice
        });
    }

    private static async Task PlaceMarketOrder(string symbol, OrderSide side, decimal quantity)
    {
        Console.WriteLine($"Placing Market Order: {symbol} {side} {quantity}");
        var order = await _orderService!.ExecuteMarketOrder(symbol, side, quantity);

        if (order != null)
        {
            Console.WriteLine($"Order placed successfully!");
            Console.WriteLine($"  Order ID: {order.OrderId}");
            Console.WriteLine($"  Status: {order.Status}");
            Console.WriteLine($"  Quantity: {order.Quantity}");
        }
        else
        {
            Console.WriteLine("Failed to place order");
        }
    }

    private static async Task PlaceLimitOrder(string symbol, OrderSide side, decimal quantity, decimal price)
    {
        Console.WriteLine($"Placing Limit Order: {symbol} {side} {quantity} @ {price}");
        var order = await _orderService!.ExecuteLimitOrder(symbol, side, quantity, price);

        if (order != null)
        {
            Console.WriteLine($"Order placed successfully!");
            Console.WriteLine($"  Order ID: {order.OrderId}");
            Console.WriteLine($"  Status: {order.Status}");
            Console.WriteLine($"  Price: {order.Price}");
            Console.WriteLine($"  Quantity: {order.Quantity}");
        }
        else
        {
            Console.WriteLine("Failed to place order");
        }
    }

    private static async Task ProcessTWAP(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Usage: twap <SYMBOL> <BUY|SELL> <TOTAL_QTY> <NUM_TRADES> [INTERVAL_SEC]");
            return;
        }

        var symbol = args[1];
        var side = ParseSide(args[2]);
        var totalQty = decimal.Parse(args[3]);
        var numTrades = int.Parse(args[4]);
        var interval = args.Length > 5 ? int.Parse(args[5]) : 60;

        await _strategyService!.ExecuteTWAP(new TWAPRequest
        {
            Symbol = symbol,
            Side = side,
            TotalQuantity = totalQty,
            NumberOfTrades = numTrades,
            IntervalSeconds = interval
        });
    }

    private static async Task ProcessOCO(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Usage: oco <SYMBOL> <BUY|SELL> <QUANTITY> <LIMIT_PRICE> <STOP_PRICE>");
            return;
        }

        var symbol = args[1];
        var side = ParseSide(args[2]);
        var quantity = decimal.Parse(args[3]);
        var limitPrice = decimal.Parse(args[4]);
        var stopPrice = decimal.Parse(args[5]);

        await _strategyService!.ExecuteOCO(new OCOOrderRequest
        {
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = limitPrice,
            StopPrice = stopPrice
        });
    }

    private static async Task ShowAccountInfo()
    {
        var info = await _orderService!.GetAccountInfo();
        if (info != null)
        {
            Console.WriteLine("╔═══════════════════════════════════════╗");
            Console.WriteLine("║         ACCOUNT INFORMATION           ║");
            Console.WriteLine("╠═══════════════════════════════════════╣");
            Console.WriteLine($"║  Total Wallet Balance: {info.TotalWalletBalance,-12:F2} ║");
            Console.WriteLine($"║  Unrealized P/L:       {info.TotalUnrealizedProfit,-12:F2} ║");
            Console.WriteLine($"║  Total Margin:        {info.TotalMarginBalance,-12:F2} ║");
            Console.WriteLine($"║  Available Balance:   {info.AvailableBalance,-12:F2} ║");
            Console.WriteLine($"║  Max Withdraw:        {info.MaxWithdrawAmount,-12:F2} ║");
            Console.WriteLine("╚═══════════════════════════════════════╝");
        }
    }

    private static async Task ShowPositions(string? symbol)
    {
        var positions = await _orderService!.GetPositions(symbol);

        if (positions.Count == 0)
        {
            Console.WriteLine("No open positions");
            return;
        }

        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                            OPEN POSITIONS                                    ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");

        foreach (var pos in positions)
        {
            var pnl = pos.UnrealizedProfit >= 0 ? $"+{pos.UnrealizedProfit:F2}" : $"{pos.UnrealizedProfit:F2}";
            Console.WriteLine($"║  {pos.Symbol,-12} | Qty: {pos.PositionAmount,8:F3} | Entry: {pos.EntryPrice,10:F2} | P/L: {pnl,12} ║");
        }
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
    }

    private static async Task ShowPrice(string symbol)
    {
        var price = await _orderService!.GetCurrentPrice(symbol);
        Console.WriteLine($"{symbol}: {price:F2} USDT");
    }

    private static async Task SetLeverage(string symbol, int leverage)
    {
        Console.WriteLine($"Setting leverage to {leverage}x for {symbol}");
        var success = await _orderService!.SetLeverage(symbol, leverage);
        Console.WriteLine(success ? "Leverage set successfully" : "Failed to set leverage");
    }

    private static async Task CancelOrder(string symbol, long orderId)
    {
        Console.WriteLine($"Cancelling order {orderId} on {symbol}");
        var success = await _orderService!.CancelOrder(symbol, orderId);
        Console.WriteLine(success ? "Order cancelled successfully" : "Failed to cancel order");
    }

    private static async Task ShowOpenOrders(string? symbol)
    {
        var orders = await _orderService!.GetOpenOrders(symbol);

        if (orders.Count == 0)
        {
            Console.WriteLine("No open orders");
            return;
        }

        Console.WriteLine("╔════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         OPEN ORDERS                                ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════════════╣");

        foreach (var order in orders)
        {
            var priceStr = order.Price.HasValue ? order.Price.Value.ToString("F2") : "Market";
            Console.WriteLine($"║  ID: {order.OrderId,-10} | {order.Symbol,-10} | {order.Side,-4} {order.Type,-8} | Qty: {order.Quantity,-6} | @ {priceStr,-8} ║");
        }
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝");
    }

    private static void ShowLogs(int count)
    {
        var logs = _logger!.GetRecentLogs(count);

        if (logs.Count == 0)
        {
            Console.WriteLine("No logs available");
            return;
        }

        Console.WriteLine($"╔═══════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                          RECENT LOGS ({count})                        ║");
        Console.WriteLine($"╠═══════════════════════════════════════════════════════════════════════╣");

        foreach (var log in logs)
        {
            var statusColor = log.Status switch
            {
                "FILLED" or "EXECUTING" => "✓",
                "CANCELLED" or "REJECTED" => "✗",
                _ => "○"
            };
            Console.WriteLine($"║ {statusColor} [{log.Timestamp:HH:mm:ss}] {log.Action,-25} | {log.Symbol,-10} ║");
            Console.WriteLine($"║   {log.Details,-70} ║");
        }
        Console.WriteLine($"╚═══════════════════════════════════════════════════════════════════════╝");
    }

    private static OrderSide ParseSide(string side)
    {
        return side.ToUpper() switch
        {
            "BUY" => OrderSide.BUY,
            "SELL" => OrderSide.SELL,
            _ => throw new ArgumentException($"Invalid side: {side}")
        };
    }

    private static void ShowHelp()
    {
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════════╗
║                      COMMAND REFERENCE                             ║
╠══════════════════════════════════════════════════════════════════╣
║  market <s> <side> <qty>     - Place market order                 ║
║  limit <s> <side> <qty> <p>  - Place limit order                   ║
║  twap <s> <side> <total> <n> [i] - TWAP execution                 ║
║  oco <s> <side> <qty> <lp> <sp> - OCO order                       ║
║  account                    - Show account info                   ║
║  positions [symbol]         - Show open positions                 ║
║  price <symbol>             - Check current price                ║
║  leverage <s> <lev>         - Set leverage                        ║
║  cancel <s> <id>            - Cancel order                        ║
║  openorders [symbol]        - Show open orders                   ║
║  logs [count]               - Show trade logs                     ║
║  help                       - Show this help                      ║
╠══════════════════════════════════════════════════════════════════╣
║  Examples:                                                        ║
║    market BTCUSDT BUY 0.01                                        ║
║    limit ETHUSDT SELL 0.5 2500                                    ║
║    twap BTCUSDT BUY 1 10 30                                        ║
║    oco BTCUSDT BUY 0.01 50000 48000                               ║
╚══════════════════════════════════════════════════════════════════╝
");
    }
}
