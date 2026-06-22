var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registreer de HTTP-client naar de achterliggende microservice
builder.Services.AddHttpClient("OrderingService", client =>
{
    // We gaan ervan uit dat de Ordering API straks op poort 5100 draait
    client.BaseAddress = new Uri("http://localhost:5100/");
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

