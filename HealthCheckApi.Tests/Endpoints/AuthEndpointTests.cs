using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using HealthCheckApi.Data;
using HealthCheckApi.Models;
using HealthCheckApi.DTOs;
using HealthCheckApi.Services;

namespace HealthCheckApi.Tests.Endpoints;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _dbName;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _dbName = Guid.NewGuid().ToString();
        -factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["JwtSettings:SecretKey"] = "ThisIsATemporaryTestKey",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ItemsDbContext>) ||
                    d.ServiceType == typeof(ItemsDbContext)||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition()== typeof(DbContextOptions<>))||
                    d.ServiceType.Name.Contains("EntityFramework") || 
                    d.ServiceType.Name.Contains("Nqgsql"))
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

    #region Authentication Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithToken()
    {
        //Arrange
        var registerRequest = new registerRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "TestPassword123",
            Roles = new List<string> {"User"}
        };

        var json = JsonSerializer.Serialize(registerRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act 
        var response = await _client.PostAsync("/auth/register", content);
        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<loginResponse>(responseContent, _jsonOptions);

        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Token);
        Assert.Equal("test@example.com", loginResponse.Email);
        Assert.Equal("Test User", loginResponse.Name);
        Assert.Contains("User", loginResponse.Roles);
    }

    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        //Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = "duplicate@example.com",
            Password = "TestPassword321",
            Roles = new List<string> {"User"}
        };

        var json = JsonSerializer.Serialize(registerRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await _client.PostAsync("/auth/register", content);

        //Act
        var duplicateContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/auth/register", duplicateContent);
        //Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("","test@example.com", "password")]
    [InlineData("Test User","", "password")]
    [InlineData("Test User","test@example.com", "")]
    public async Task Register_WithMissingFields_ReturnsBadRequest(string name, string email, string password)
    {
        //Arrange
        var registerRequest = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };

        var json = JsonSerializer.Serialize(registerRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act
        var response = await _client.PostAsync("/auth/register", content);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        //Arrange
       var registerRequest = new RegisterRequest("Login User", "login@example.com", "Password123!", new List<string> { "User" });
       var json = JsonSerializer.Serialize(registerRequest, _jsonOptions);
       var content = new StringContent(json, Encoding.UTF8, "application/json");
       var response = await _client.PostAsync("/auth/register", content);
       Assert.Equal(HttpStatusCode.Created, response.HttpStatusCode);


        var loginRequest = new loginRequest
        {
            Email = "login@example.com",
            Password = "LoginPassword123"
        };

        var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act
        var response = await _client.PostAsync("/auth/login", content);
        //Assert
        Assert.Equal(HttpStatusCode.Ok, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<loginResponse>(responseContent, _jsonOptions);

        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Token);
        Assert.Equal("login@example.com", loginResponse.Email);
        Assert.Equal("Login User", loginResponse.Name);
    }
    
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        //Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //Act
        var repsonse = await _client.PostAsync("/auth/login", content);
        //Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
    }

    [Theory]
    [InlineData("","Password")]
    [InlineData("test@example.com", "")]
    public async Task Login_WithMissingFields_ReturnsBadRequest(string email, string password)
    {
        //Arrange
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //Act 
        var repsonse = await _client.PostAsync("/auth/login", content);
        //Assert 
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion;

    #region Authorization Tests

}
