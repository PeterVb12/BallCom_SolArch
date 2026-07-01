using BallCom.Warehouse.API.Data;
using BallCom.Warehouse.API.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = "Host=localhost;Port=5435;Database=warehouse_db;Username=warehouse_user;Password=warehouse_password";
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddHttpClient("OrderingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5100/");
});

builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

builder.Services.AddHostedService<PaymentCompletedEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
