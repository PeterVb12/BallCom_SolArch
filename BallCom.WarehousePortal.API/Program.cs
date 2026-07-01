var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddHttpClient("WarehouseService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Warehouse"] ?? "http://localhost:5500/");
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
