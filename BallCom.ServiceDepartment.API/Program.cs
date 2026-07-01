using BallCom.ServiceDepartment.API.Data;
using BallCom.ServiceDepartment.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = "Host=localhost;Port=5436;Database=service_department_db;Username=service_user;Password=service_password";
builder.Services.AddDbContext<ServiceDepartmentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient("OrderingService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5100/");
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5400/");
});

builder.Services.AddHttpClient("WarehouseService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5500/");
});

builder.Services.AddScoped<OrderStatusAggregator>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDepartmentDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
