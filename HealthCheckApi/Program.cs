using Microsoft.EntityFrameworkCore;
using HealthCheckApi.Models;
using HealthCheckApi.Data;
using HealthCheckApi.Services;
using Npgsql;
using HealthCheckApi.Extensions;
using HealthCheckApi.DTOs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);



//Bind Kestrel to Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

//configure connection string; DatabaseURL-->Npgsql 

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    connectionString = ConvertDbUrlToNpgsql(databaseUrl);
}
else 
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("No database connection string configured.");
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;



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
    options.UseNpgsql(connectionString));
}
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ItemsDbContext>();

//Swagger
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

    //Add JWT to swagger
    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement)
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
});

//configure JWT authentication
var jwt = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key), 
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"], 
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateLifetime = true, 
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AllRoles", policy => policy.RequireRole("Admin","Pharmacist", "Nurse", "Physician", "SupplyChain", "Clerical", "User"));
    options.AddPolicy("ModifyInventory", policy => policy.RequireRole("Admin", "Pharmacist", "SupplyChain", "User"));
    options.AddPolicy("ViewOnly", policy => policy.RequireRole("Physician", "Clerical"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();


using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ItemsDbContext>();
context.Database.Migrate();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}
//helper method
static string ConvertDbUrlToNpgsql(string url)
{
    //format: postgres://user:pass@host:port/db
    var uri = new Uri(url);
    var user = uri.UserInfo.Split(':')[0];
    var pass = uri.UserInfo.Split(':')[1];
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SslMode=Require;Trust Server Certificate=true";
}

// Get logger 
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


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
.RequireAuthorization("AllRoles")
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

// Register endpoint 
app.MapPost("/auth/register", async (RegisterRequest request, UserService userService, TokenService tokenService) =>
{
    logger.LogInformation("Registration attempt for email: {Email}", request.Email);

    if(string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
    {
        return Results.BadRequest("Name, email, and password are required");
    }

    try
    {
        var createUserDto = new CreateUserDto(request.Name, request.Email, request.Password);
        var user = await userService.CreateUserAsync(createUserDto);

        if(user == null)
        {
            logger.LogWarning("Registration failed: User already exists with email {Email}", request.Email);
            return Results.Conflict("User with this email already exists");
        }

        //generate new token
        var token = tokenService.GenerateToken(user.Id.ToString(), user.Email, new[]{user.Role});
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        logger.LogInformation("User registered successfully: {UserId}", user.Id);

        return Results.Created($"/users/{user.Id}", new LoginResponse(
            token,
            user.Email,
            user.Name,
            user.Role,
            expiresAt
        ));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
        return Results.Problem("An error occured during registration", statusCode: 500);
    }
})
.WithName("Register")
.WithSummary("User registration")
.WithDescription("registers a new user and return a JWT token")
.Produces<LoginResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status409Conflict)
.WithOpenApi();

//Login endpoint
app.MapPost("/auth/login", async (LoginRequest request, UserService userService, TokenService tokenService) =>
{
    logger.LogInformation("Login attempt for email: {Email}", request.Email);

    if(string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest("Email and Password are required");
    }

    try
    {
        var user = await userService.ValidateUserAsync(request.Email, request.Password);
        if(user == null)
        {
            logger.LogWarning("login failed for email: {Email}", request.Email);
            return Results.Unauthorized();
        }

        //generate token
        var token = tokenService.GenerateToken(user.Id.ToString(), user.Email, new[]{user.Role});
        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return Results.Ok(new LoginResponse(
            token,
            user.Email,
            user.Name,
            user.Role,
            expiresAt
        ));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during login for email: {Email}", request.Email);
        return Results.Problem("An error occured during login", statusCode: 500);
    }
})
.WithName("Login")
.WithSummary("User login")
.WithDescription("AUthenticates user and return jwt")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.WithOpenApi();

//Get current user info
app.MapGet("/auth/me", async (HttpContext httpContext, UserService userService) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if(string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var user = await userService.GetUserByIdAsync(UserId);
        if (user ==null)
        {
            return Results.NotFound("User not found");
        }

        return Results.Ok(user.ToResponseDto());
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retreiving user profile for userId: {UserId}", userId);
        return Results.Problem("An error occured while retreving user Profile", statusCode: 500);
    }
})
.RequireAuthorization("AllRoles")
.WithName("GetCurrentUser")
.WithSummary("Get current user profile")
.WithDescription("Returns the profile of the currently authenticated user")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();


// GET / items  ( filtering & pagination)
app.MapGet("/items", async (
            ItemsDbContext db,
            string? search, 
            int? minQuantity,
            int? maxQuantity,
            int  page = 1,
            int  pageSize = 10) =>
            {
                if(page <= 0 || pageSize <= 0 || pageSize > 100)
                return Results.BadRequest("Invalid paging parameters");

                IQueryable<Item> q = db.Items;

                if(!string.IsNullOrWhiteSpace(search))
                    q = q.Where(i => i.Name.Contains(search));
                if(minQuantity.HasValue)
                    q = q.Where(i => i.Quantity >= minQuantity);
                if(maxQuantity.HasValue)
                    q = q.Where(i => i.Quantity <= maxQuantity);

                int total = await q.CountAsync();

                var items = await q.OrderBy(i => i.Id)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .Select(i => i.ToResponseDto())
                                    .ToListAsync();
                var response = new PagedResponse<ItemResponseDto>(items, page, pageSize, total);

                return Results.Ok(response);
            })
            .RequireAuthorization("AllRoles")
            .WithName("GetItems")
            .WithSummary("List items with optional filters & paging")
            .Produces<PagedResponse<ItemResponseDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);


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
.RequireAuthorization("ModifyInventory")
.WithName("CreateItem")
.WithSummary("Create a new item")
.WithDescription("Creates a new item with the provided name and quantity")
.Produces<ItemResponseDto>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
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
.RequireAuthorization("ModifyInventory")
.WithName("UpdateItem")
.WithSummary("Update an item")
.WithDescription("Updates an existing item by its ID with the provided name and quantity")
.Produces<ItemResponseDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status401Unauthorized)
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
.RequireAuthorization("AdminOnly")
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

