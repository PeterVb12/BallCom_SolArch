using BallCom.Catalog.API.Commands;
using BallCom.Catalog.API.Data;
using BallCom.Catalog.API.Messaging;
using BallCom.Catalog.API.Queries;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Connectie naar de eigen Postgres container van de Catalog Service in Docker.
// Eigen database (catalog_db) op poort 5433 om conflict met ordering_db te vermijden.
var connectionString = "Host=localhost;Port=5433;Database=catalog_db;Username=catalog_user;Password=catalog_password";
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
