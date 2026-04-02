using BinanceTrading.TradingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BinanceApiService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingService" }));

Console.WriteLine("Trading Service is running on http://localhost:5002");

app.Run();
