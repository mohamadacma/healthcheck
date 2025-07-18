using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;  
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using HealthCheckApi.Data;
using HealthCheckApi.Models;
using HealthCheckApi.DTOs;


namespace HealthCheckApi.Tests.Endpoints;

public class ItemsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _dbName;

    public ItemsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _dbName = Guid.NewGuid().ToString();
        

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
                 
            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                .Where(d => 
                d.ServiceType == typeof(DbContextOptions<ItemsDbContext>) ||
                d.ServiceType == typeof(ItemsDbContext) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                d.ServiceType.Name.Contains("EntityFramework") ||
                d.ServiceType.Name.Contains("Npgsql")
                )
                .ToList();
            
                foreach (var descriptor in descriptorsToRemove)
                
                    services.Remove(descriptor);
                
                services.AddDbContext<ItemsDbContext>(options =>
                
                options.UseInMemoryDatabase(_dbName));
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
        Assert.NotNull(returnedItem);

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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_WithNonExistentId_ReturnsNotFound()
    {
        //Arrange
        int nonExistentId = 1298;
        //Act
        var response = await _client.GetAsync($"/items/{nonExistentId}");
        //Assert 
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    #endregion

       #region GET /items/ Tests

       [Fact]
       public async Task GetItems_ReturnsAllItems()
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
            Assert.NotNull(items);
            Assert.True(items.Count >=2);
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
        #endregion

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
            Assert.NotNull(createdItem);

            Assert.Equal("New Item", createdItem.Name);
            Assert.Equal(15, createdItem.Quantity);
            Assert.True(createdItem.Id > 0);

            Assert.Equal($"/items/{createdItem.Id}", response.Headers.Location?.ToString());
       }


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

       [Fact]
       public async Task CreateItem_WithNullDto_ReturnsBadRequest()
        {
            //Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PostAsync("/items", content);
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

       #endregion;

       #region PUT /items/{id} Tests

       [Fact]
       public async Task UpdateItem_WithValidData_ReturnsOk()
       {
            //Arrange
            var item = await CreateTestItem("Existing Item", 5);
            var dto = new UpdateItemDto { Name = "Updated Item", Quantity = 20 };
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PutAsync($"/items/{item.Id}", content);
            //Assert
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedItem = JsonSerializer.Deserialize<ItemResponseDto>(responseContent, _jsonOptions);
            Assert.NotNull(updatedItem);
            Assert.Equal(item.Id, updatedItem.Id); 
            Assert.Equal("Updated Item", updatedItem.Name);
            Assert.Equal(20, updatedItem.Quantity);
       }

       [Theory]
       [InlineData(0)]
       [InlineData(-1)]
       public async Task UpdateItem_WithInvalidId_ReturnsBadRequest(int invalidId)
       {
            //Arrange
            var dto = new UpdateItemDto { Name = "Updated Item", Quantity = 20 };
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PutAsync($"/items/{invalidId}", content);
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
       }

       [Fact]
        public async Task UpdateItem_WithNonExistentId_ReturnsNotFound()
        {
            var dto = new UpdateItemDto { Name = "Updated Item", Quantity = 20 };
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PutAsync("/items/777", content);
            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateItem_WithNullDto_ReturnsBadRequest()
        {
            //Arrange
            var item = await CreateTestItem("Test Item", 5);
            var content = new StringContent("null", Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PutAsync($"/items/{item.Id}", content);
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateItem_WithNegativeQuantity_ReturnsBadRequest()
        {
            //Arrange
            var item = await CreateTestItem("Test Item", 5);
            var dto = new UpdateItemDto { Name = "Updated Item", Quantity = -1 };
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //Act
            var response = await _client.PutAsync($"/items/{item.Id}", content);
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        #endregion

        #region DELETE /items/{id} Tests

        [Fact]
        public async Task DeleteItem_WithValidId_ReturnsDeleted()
        {
            //Arrange
            var item = await CreateTestItem("Item to Delete", 6);
            //Act
            var response = await _client.DeleteAsync($"/items/{item.Id}");
            //Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var getResponse = await _client.GetAsync($"/items/{item.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-999)]
        public async Task DeleteItem_WithInValidId_ReturnsBadRequest(int invalidId)
        {
            //Act
            var response = await _client.DeleteAsync($"/items/{invalidId}");
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_WithNonExistentId_ReturnsNotFound()
        {
            //Act
            var response = await _client.DeleteAsync($"/items/999");
            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        
        #endregion

        #region Helper Methods

        private async Task<Item> CreateTestItem(string name, int quantity)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ItemsDbContext>();
            var item = new Item { Name = name, Quantity = quantity };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            return item;

        }
        #endregion
}








    