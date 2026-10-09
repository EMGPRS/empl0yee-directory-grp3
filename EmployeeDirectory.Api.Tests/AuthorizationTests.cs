using System.Net;
using System.Net.Http.Json;
using EmployeeDirectory.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace EmployeeDirectory.Api.Tests;

public sealed class AuthorizationTests : IClassFixture<EmployeeApiFactory>
{
    private readonly EmployeeApiFactory _factory;

    public AuthorizationTests(EmployeeApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEmployees_WithoutLogin_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/employee");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CanReadEmployees()
    {
        using var client = CreateClient();
        await LoginAsync(client, "viewer", "viewer123");

        var response = await client.GetAsync("/api/employee");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotDeleteEmployee()
    {
        using var client = CreateClient();
        await LoginAsync(client, "viewer", "viewer123");
        _factory.EmployeeService.Invocations.Clear();

        var response = await client.DeleteAsync("/api/employee/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.EmployeeService.Verify(
            service => service.DeleteEmployeeAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Admin_CanDeleteEmployee()
    {
        using var client = CreateClient();
        await LoginAsync(client, "admin", "admin123");
        _factory.EmployeeService.Invocations.Clear();

        var response = await client.DeleteAsync("/api/employee/1");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        _factory.EmployeeService.Verify(
            service => service.DeleteEmployeeAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task InvalidCredentials_ReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "viewer", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task LoginAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        response.EnsureSuccessStatusCode();
    }
}

public sealed class EmployeeApiFactory : WebApplicationFactory<Program>
{
    public Mock<IEmployeeService> EmployeeService { get; } = CreateEmployeeService();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmployeeService>();
            services.AddSingleton(EmployeeService.Object);
        });
    }

    private static Mock<IEmployeeService> CreateEmployeeService()
    {
        var service = new Mock<IEmployeeService>();
        service.Setup(value => value.GetEmployeesAsync())
            .ReturnsAsync(new List<Employee>());
        service.Setup(value => value.DeleteEmployeeAsync(1))
            .ReturnsAsync(true);
        return service;
    }
}
