using BallCom.Catalog.API.Commands;
using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Messaging;
using BallCom.Catalog.API.Queries;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

builder.Services.AddScoped<AddProductCommandHandler>();
builder.Services.AddScoped<CatalogQueryHandler>();
builder.Services.AddScoped<UpdateProductCommandHandler>();

builder.Services.AddScoped<ReplayProductsCommandHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
