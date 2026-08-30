using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ECommerce.Api.Tests;

public class AdminPermissionsTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminPermissionsTests()
    {
        Environment.SetEnvironmentVariable("JwtSettings__Secret", "SuperSecureTestSecret123!SuperSecureTestSecret123!", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "CleanArchitectureApi", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "CleanArchitectureApiClient", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("JwtSettings__ExpiresInMinutes", "60", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("AdminSettings__Username", "admin", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("AdminSettings__Email", "admin@ecommerce.com", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("AdminSettings__Password", "Admin123!", EnvironmentVariableTarget.Process);

        _factory = new WebApplicationFactory<Program>();
    }

    [Fact]
    public async Task Admin_can_create_and_delete_a_product()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "Admin123!"
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        var createResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Producto de prueba",
            description = "Creado por test de admin",
            price = 100.50m,
            stock = 10,
            categoryId = 1
        });

        createResponse.EnsureSuccessStatusCode();

        var product = await createResponse.Content.ReadFromJsonAsync<ProductResult>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(product);
        Assert.True(product.Id > 0);
        Assert.Equal("Producto de prueba", product.Name);

        var deleteResponse = await client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private sealed record LoginResult(string Token, string Username, string Email, string Role);
    private sealed record ProductResult(int Id, string Name, string Description, decimal Price, int Stock);
}
