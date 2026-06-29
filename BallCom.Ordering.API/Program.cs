using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Connectie naar de Postgres container in Docker
var connectionString = "Host=localhost;Port=5432;Database=ordering_db;Username=ballcom_user;Password=ballcom_password";
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();
// Start de RabbitMQ Consumer op de achtergrond
builder.Services.AddHostedService<RabbitMQEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
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

