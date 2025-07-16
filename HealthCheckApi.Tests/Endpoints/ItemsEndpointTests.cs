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

    