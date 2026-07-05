using BallCom.Ordering.API.Application.Commands;
using BallCom.Ordering.API.Application.Queries;
using BallCom.Ordering.API.Data;
using BallCom.Ordering.API.Messaging;
using BallCom.Ordering.API.Projections;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

// CQRS: gescheiden DbContexts voor de schrijf- (events) en leeskant (read models).
builder.Services.AddDbContext<OrderingWriteDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<OrderingReadDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Event Sourcing infrastructuur.
builder.Services.AddScoped<OrderEventStore>();
builder.Services.AddScoped<ReadModelRebuilder>();

// CQRS command-/query-handlers.
builder.Services.AddScoped<PlaceOrderCommandHandler>();
builder.Services.AddScoped<MarkOrderPaidCommandHandler>();
builder.Services.AddScoped<CancelOrderCommandHandler>();
builder.Services.AddScoped<OrderQueryHandler>();

// Interne async projectie-queue + achtergrondprojector (leeskant, eventueel consistent).
builder.Services.AddSingleton<ProjectionQueue>();
builder.Services.AddHostedService<OrderProjectionService>();

// Event Driven Architecture (cross-service via RabbitMQ).
builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddHostedService<RabbitMQEventConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var writeContext = scope.ServiceProvider.GetRequiredService<OrderingWriteDbContext>();
    await OrderingDbInitializer.InitializeAsync(writeContext);
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
