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

builder.Services.AddDbContext<OrderingWriteDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<OrderingReadDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddScoped<OrderEventStore>();
builder.Services.AddScoped<ReadModelRebuilder>();

builder.Services.AddScoped<PlaceOrderCommandHandler>();
builder.Services.AddScoped<MarkOrderPaidCommandHandler>();
builder.Services.AddScoped<CancelOrderCommandHandler>();
builder.Services.AddScoped<OrderQueryHandler>();

builder.Services.AddSingleton<ProjectionQueue>();
builder.Services.AddHostedService<OrderProjectionService>();

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
