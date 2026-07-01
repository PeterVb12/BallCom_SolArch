var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient("CustomerService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CustomerService"] ?? "http://localhost:5700/");
});

builder.Services.AddHttpClient("LogisticsService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Logistics"] ?? "http://localhost:5900/");
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
