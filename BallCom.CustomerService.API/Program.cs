using BallCom.CustomerService.API.Data;
using BallCom.CustomerService.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
builder.Services.AddDbContext<CustomerServiceDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient("OrderingService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Ordering"] ?? "http://localhost:5100/");
});

builder.Services.AddHttpClient("LogisticsService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Logistics"] ?? "http://localhost:5900/");
});

builder.Services.AddScoped<InquiryStatusAggregator>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerServiceDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
