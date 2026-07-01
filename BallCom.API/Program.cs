var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient("OrderingService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Ordering"] ?? "http://localhost:5100/");
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Payment"] ?? "http://localhost:5400/");
});

builder.Services.AddHttpClient("LogisticsService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Logistics"] ?? "http://localhost:5900/");
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
