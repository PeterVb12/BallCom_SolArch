using BallCom.Catalog.API.Commands;
using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Messaging;
using BallCom.Catalog.API.Queries;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

// Added a CQRS layer
builder.Services.AddScoped<AddProductCommandHandler>();
builder.Services.AddScoped<CatalogQueryHandler>();
builder.Services.AddScoped<UpdateProductCommandHandler>();

// Added a Command to replay (event sourcing)
builder.Services.AddScoped<ReplayProductsCommandHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.Run();
