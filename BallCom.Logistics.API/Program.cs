using BallCom.Logistics.API.Data;
using BallCom.Logistics.API.Messaging;
using BallCom.Logistics.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient("OrderingService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Ordering"] ?? "http://localhost:5100/");
});

builder.Services.AddScoped<CarrierSelectionService>();
builder.Services.AddScoped<CarrierStatusProvider>();

builder.Services.AddControllers();
builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddHostedService<PackageReadyEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
