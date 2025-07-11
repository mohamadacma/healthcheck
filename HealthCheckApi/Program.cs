using Microsoft.EntityFrameworkCore;
using HealthCheckApi.Models;
using HealthCheckApi.Data;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi(); 
builder.Services.AddDbContext<ItemsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ItemsDbContext>();
    

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// GET and POST endpoints to manage Item data
app.MapGet("/items/{id}", async (ItemsDbContext context, int id) =>
{
    //validate Id
    if(id <=0) 
    return Results.BadRequest("ID must be greater than 0");

    var item = await context.Items.FindAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

app.MapGet("/items", async (ItemsDbContext context) =>
    await context.Items.ToListAsync())
    .WithName("GetItems");

app.MapPost("/items", async (ItemsDbContext context, Item item) =>
{
    if(string.IsNullOrWhiteSpace(item.Name))
    return Results.BadRequest("Name is required");

    if(item.Quantity < 0)
    return Results.BadRequest("Quantity cannot be negative");

    if(item.Name.length > 100)
    return Results.BadRequest("Name cannot exceed 100 characters");


    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
})
.WithName("CreateItem");

//endpoints to monitor app and db health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

