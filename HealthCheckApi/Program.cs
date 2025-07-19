using Microsoft.EntityFrameworkCore;
using HealthCheckApi.Models;
using HealthCheckApi.Data;
using Npgsql;
using HealthCheckApi.Extensions;
using HealthCheckApi.DTOs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var isTestEnvironment = builder.Environment.EnvironmentName == "Test" || 
builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

if (isTestEnvironment)
{
    builder.Services.AddDbContext<ItemsDbContext>(options =>
    options.UseInMemoryDatabase("DefaultTestDb"));
}
else
{
builder.Services.AddDbContext<ItemsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ItemsDbContext>();

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Items Management API",
        Version = "v1.0.0",
        Description = "RESTful API for managing inventory items " +
                    "with CRUD operations, health monitoring, and error handling.",
        Contact = new() { Name = "Moe", Email = "moreborn2021@gmail.com" }
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //Dev only automation
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ItemsDbContext>();
    context.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI(); 
}

// Get logger 
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
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


// GET to retireve item by ID
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
        return Results.Ok(item.ToResponseDto());
    } 
    catch (NpgsqlException ex)
    {
        logger.LogError(ex, "Unexpected error occurred while retrieving item with ID: {ItemId}", id);
        return Results.Problem("An unexpected error occured", statusCode: 500);
    }
})
.WithName("GetItem")
.WithSummary("Get item by ID")
.WithDescription("Retrieves a specific item by its unique identifier")
.Produces<ItemResponseDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi(operation => 
{
    operation.Parameters[0].Description = "The unique identifier of the item";
    return operation;
});


//GET to retrieve all items
app.MapGet("/items", async (ItemsDbContext context) =>
 {
    logger.LogInformation("Retreiving all items");

    try 
    {
        var items= await context.Items.ToListAsync();
        logger.LogInformation("Successfully retrieved {ItemCount} items", items.Count);
        return Results.Ok(items.Select(item => item.ToResponseDto()));
    }
    catch (NpgsqlException ex)
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
    .WithName("GetItems")
    .WithSummary("Get all items")
    .WithDescription("Retrieves a list of all items in the database")
    .Produces<IEnumerable<ItemResponseDto>>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status500InternalServerError)
    .WithOpenApi();


//POST to create a new item
app.MapPost("/items", async (ItemsDbContext context, CreateItemDto dto) => {
    logger.LogInformation("Creating new item: {ItemName}", dto?.Name);
    //validate
    if(dto == null) 
    {
        logger.LogWarning("Item creation failed: null item provided");
        return Results.BadRequest("Item data is required");
    }
    if(string.IsNullOrWhiteSpace(dto.Name)) 
    {
        logger.LogWarning("Item creation failed: name is null or empty");
        return Results.BadRequest("Name is required");
    }

    if(dto.Quantity < 0) 
    {
        logger.LogWarning("Item creation failed: negative quantity not allowed");
        return Results.BadRequest("Quantity cannot be negative");
    }

    if(dto.Name.Length > 100) 
    {
        logger.LogWarning("Item creation failed: name is too long");
        return Results.BadRequest("Name cannot exceed 100 characters");
    }

    try 
    {
        var item = dto.ToItem();
        context.Items.Add(item);
        await context.SaveChangesAsync();
        logger.LogInformation("Successfully created item with ID: {ItemId}", item.Id);
        return Results.Created($"/items/{item.Id}", item.ToResponseDto());
    }
    catch (DbUpdateException ex)
    {
         logger.LogError(ex, "Database update error occurred while creating item");
         return Results.Problem("Failed to save item to database", statusCode: 500);
    }
    catch (NpgsqlException ex)
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
.WithName("CreateItem")
.WithSummary("Create a new item")
.WithDescription("Creates a new item with the provided name and quantity")
.Produces<ItemResponseDto>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.WithOpenApi();


//PUT to update item
app.MapPut("/items/{id}", async (ItemsDbContext context, int id, UpdateItemDto dto) =>
{
    logger.LogInformation("Updating item with ID: {ItemId}", id);

    //validate ID
    if (id <= 0)
    {
        logger.LogWarning("Invalid item ID for update: {ItemId}", id);
        return Results.BadRequest("ID must be greater than 0");
    }
    //validate input
    if(dto == null)
    {
        logger.LogWarning("Update failed: empty name for ID: {ItemId}", id);
        return Results.BadRequest("Name is required");
    }

    if (dto.Quantity < 0    )
    {
         logger.LogWarning("Update failed: name too long for ID: {ItemId}", id);
         return Results.BadRequest("Quantity cannot be negative");
    }

    try
    {
         var existingItem = await context.Items.FindAsync(id);
         if (existingItem == null)
         {
            logger.LogWarning("Update failed: item not found with ID: {ItemId}", id);
            return Results.NotFound($"Item with ID {id} not found");
         }
        //update item
         existingItem.UpdateFromDto(dto);
        
        //save changes
          await context.SaveChangesAsync();

          logger.LogInformation("Successfully updated item with ID: {ItemId}", id);
          return Results.Ok(existingItem.ToResponseDto());
    }
   catch (DbUpdateException ex)
   {
    logger.LogError(ex, "Database update error occurred while updating item with ID: {ItemId}", id);
    return Results.Problem("Failed to update item in database", statusCode: 500);
   }
    catch (NpgsqlException ex)
    {
        logger.LogError(ex, "Database error occurred while updating item with ID: {ItemId}", id);
         return Results.Problem("Database error occurred", statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error occurred while updating item with ID: {ItemId}", id);
         return Results.Problem("An unexpected error occurred", statusCode: 500);
    }
})
.WithName("UpdateItem")
.WithSummary("Update an item")
.WithDescription("Updates an existing item by its ID with the provided name and quantity")
.Produces<ItemResponseDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "The unique identifier of the item";
    return operation;
});


//DELETE to remove item
app.MapDelete("/items/{id}", async(ItemsDbContext context, int id)=>
{
    logger.LogInformation("Deleting item with ID: {ItemId}", id);

    if (id <= 0)
    {
        logger.LogWarning("Invalid item ID for deletion: {ItemId}", id);
        return Results.BadRequest("ID must be greater than 0");
    }

    try
    {
        var item = await context.Items.FindAsync(id);
        if (item == null)
        {
            logger.LogWarning("Delete failed: item not found with ID: {ItemId}", id);
            return Results.NotFound($"Item with ID {id} not found");
        }
        //remove item
        context.Items.Remove(item);

        //save changes
        await context.SaveChangesAsync();

        logger.LogInformation("Successfully deleted item with ID: {ItemId}", id);
        return Results.NoContent();
    }
    catch (DbUpdateException ex)
    {
        logger.LogError(ex, "Database update error occurred while deleting item with ID: {ItemId}", id);
        return Results.Problem("Failed to delete item from database", statusCode: 500);
    }
    catch (NpgsqlException ex)
    {
        logger.LogError(ex, "Database error occurred while deleting item with ID: {ItemId}", id);
        return Results.Problem("Database error occurred", statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error occurred while deleting item with ID: {ItemId}", id);
        return Results.Problem("An unexpected error occurred", statusCode: 500);
    }
})
.WithName("DeleteItem")
.WithSummary("Delete an item")
.WithDescription("Deletes an item from the database by its unique identifier")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status500InternalServerError)
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "The unique identifier of the item";
    return operation;
});



//endpoints to monitor app and db health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }

