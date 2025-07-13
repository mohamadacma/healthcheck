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

// Get logger 
var logger = app.Services.GetRequiredService<ILogger<Program>>();

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
    logger.LogInformation("Retrieving item with ID: {ItemId}", id);

    //validate Id
    if(id <=0) 
    {
        logger.LogWarning("Invalid item ID requested: {ItemId}", id);
        return Results.BadRequest("ID must be greater than 0");
    }

    try
     {
        var item = await context.Items.FindAsync(id);
        if(item is null)
        {
            logger.LogWarning("Item not found with ID: {ItemId}", id);
            return Results.NotFound($"Item with ID {id} not found");
        }

        logger.LogInformation("Successfully retreived item: {ItemId}", id);
        return Results.Ok(item);
    } 
    catch (SqlException ex)
    {
        logger.LogError(ex, "Unexpected error occurred while retrieving item with ID: {ItemId}", id);
        return Results.Problem("An unexpected error occured", statusCode: 500);
    }
})
.WithName("GetItem");


//Get to retrieve all items
app.MapGet("/items", async (ItemsDbContext context) =>
 {
    logger.LogInformation("Retreiving all items");

    try 
    {
        var items= await context.Items.ToListAsync();
        logger.LogInformation("Sucessfully retreived items", items);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error occured while retrieving all items");
        return Results.Problem("Database error occurred", statusCode: 500);
    }
    catch (Exception ex)
    {
         logger.LogError(ex, "Unexpected error occurred while retrieving all items");
         return Results.Problem("An unexpected error occurred", statusCode: 500);
    }
 })
    .WithName("GetItems");

//POST to create a new item
app.MapPost("/items", async (ItemsDbContext context, Item item) => {
    logger.LogInformation("Creating new item: {ItemName}", item?.Name);
    //validate
    if(item == null) 
    {
        logger.LogWarning("Item creation failed: null item provided");
        return Results.BadRequest("Item data is required");
    }
    if(string.IsNullOrWhiteSpace(item.Name)) 
    {
        logger.LogWarning("Item creation failed: name is null or empty");
        return Results.BadRequest("Name is required");
    }

    if(item.Quantity < 0) 
    {
        logger.LogWarning("Item creation failed: negative quantity not allowed");
        return Results.BadRequest("Quantity cannot be negative");
    }

    if(item.Name.Length > 100) 
    {
        logger.LogWarning("Item creation failed: name is too long");
        return Results.BadRequest("Name cannot exceed 100 characters");
    }

    try 
    {
        context.Items.Add(item);
        await context.SaveChangesAsync();
        logger.LogInformation("Successfully created item with ID: {ItemId}", item.Id);
        return Results.Created($"/items/{item.Id}", item);
    }
    catch (dbUpdateException ex)
    {
         logger.LogError(ex, "Database update error occurred while creating item");
         return Results.Problem("Failed to save item to database", statusCode: 500);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error occurred while creating item");
        return Results.Problem("Database error occurred", statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error occurred while creating item");
        return Results.Problem("An unexpected error occurred", statusCode: 500);
    }
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

