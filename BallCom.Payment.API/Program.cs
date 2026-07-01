using BallCom.Payment.API.Data;
using BallCom.Payment.API.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Connectie naar de eigen Postgres container van de Payment Service in Docker.
// Eigen database (payment_db) op poort 5434 om conflicten met andere db's te vermijden.
var connectionString = "Host=localhost;Port=5434;Database=payment_db;Username=payment_user;Password=payment_password";
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

// Event Driven Architecture: achtergrond-consumer voor OrderPlacedEvent.
builder.Services.AddHostedService<OrderPlacedEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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
