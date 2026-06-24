var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enterprise Integration Pattern - Messaging Gateway:
// Het supplier-portaal is een dunne BFF die requests doorzet naar de Catalog microservice.
builder.Services.AddHttpClient("CatalogService", client =>
{
    // De Catalog API draait op poort 5200
    client.BaseAddress = new Uri("http://localhost:5200/");
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.Run();
