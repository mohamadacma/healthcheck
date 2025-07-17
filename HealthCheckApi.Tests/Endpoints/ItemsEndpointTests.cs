using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using HealthCheckApi.Data;
using HealthCheckApi.Models;
using HealthCheckApi.DTOs;


namespace HealthCheckApi.Tests.Endpoints;

public class ItemsEndpointTests : IClassFixture<WebApplicationFactory<Program>>

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ItemsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ItemsDbContext>));
                if (descriptor != null)
                 services.Remove(descriptor);
                 services.AddDbContext<ItemsDbContext>(options =>
                 options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        });
        
        _client = _factory.CreateClient();

        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    #region GET /items/{id} Tests

    [Fact]
    public async Task GetItem_WithValidId_ReturnsItem()
    {
        //Arrange
        var item = await CreateTestItem("Test Item", 10);
        //Act
        var response = await _client.GetAsync($"/items/{item.Id}");
        //Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var returnedItem = JsonSerializer.Deserialize<ItemResponseDto>(content, _jsonOptions);

        Assert.Equal(item.Id, returnedItem.Id);
        Assert.Equal("Test Item", returnedItem.Name);
        Assert.Equal(10, returnedItem.Quantity);
    }

    [Fact]
    public async Task GetItem_withInvalidId_ReturnsBadRequest()
    {
        //Arrange
        int invalidId = -3;
        //Act
        var response = await _client.GetAsync($"/items/{invalidId}");
        //Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.statusCode);
    }

    [Fact]
    public async Task GetItem_WithNonExistentId_ReturnsNotFound()
    {
        //Arrange
        int nonExistentId = 1298;
        //Act
        var response = await _client.GetAsync($"/items/{nonExistentId}");
        //Assert 
        Assert.Equal(HttpStatusCode.NotFound, response.statusCode);
    }
    #endregion

       #region GET /items/ Tests

       [Fact]
       public async GetItems_ReturnsAllItems()
        {
            //Arrange 
            await CreateTestItem("itemOne", 3);
            await CreateTestItem("itemTwo", 2);
            //Act
            var response = await _client.GetAsync($"/items");
            //Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<ItemResponseDto>>(content, _jsonOptions);

            Assert.True(item.Count >=2);
            Assert.Contains(items, i => i.Name == "itemOne");
            Assert.Contains(items, i => i.Name == "itemTwo");
        }
       
       [Fact]
       public async Task GetItems_WithEmptyDatabase_ReturnsEmptyList()
       {
            //Act
            var response = await _client.GetAsync($"/items");
            //Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<ItemResponseDto>>(content, _jsonOptions);

            Assert.NotNull(items);
            Assert.Empty(items);
       }
       #endregion;

       #region POST /items/ Tests
       [Fact]
       public async Task CreateItem_WithValidData_ReturnsCreated()
       {
        //Arrange
        var dto = new CreateItemDto { Name = "New Item", Quantity = 15 };
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //Act
        var response = await _client.PostAsync("/items", content);
        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdItem = JsonSerializer.Deserialize<ItemResponseDto>(responseContent, _jsonOptions);

        Assert.Equal("New Item", createdItem.Name);
        Assert.Equal(15, createdItem.Quantity);
        Assert.True(createdItem.Id > 0);

        Assert.Equal($"/items/{createdItem.Id}", response.Headers.Location?.ToString());
       }
       #endregion;

       [Theory]
       [InlineData("")] 
       [InlineData("   ")]
       [InlineData(null)]
       public async Task CreateItem_WithInvalidName_ReturnsBadRequest(string invalidName)
       {
        //Arrange
        var dto = new CreateItemDto { Name = invalidName, Quantity = 5 };
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //Act
        var response = await _client.PostAsync("/items", content);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
       }

       [Fact]
       public async Task CreateItem_WithNegativeQuantity_ReturnsBadRequest()
       {
        //Arrange
        var dto = new CreateItemDto { Name = "testItem", Quantity = -1 };
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //Act
        var response = await _client.PostAsync("/items", content);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
       }

       [Fact]
       public async Task CreateItem_WithLongName_ReturnsBadRequest()
       {
        //Arrange
        var dto = new CreateItemDto { Name = new string('A', 101), Quantity = 5 };
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //Act
        var response = await _client.PostAsync("/items", content);
        //Assert
         Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
       }

       #endregion;
       




    