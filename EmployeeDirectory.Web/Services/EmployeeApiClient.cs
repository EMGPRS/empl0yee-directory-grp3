using System.Net.Http.Json;
using EmployeeDirectory.Web.Authorization;
using EmployeeDirectory.Web.Models;

namespace EmployeeDirectory.Web.Services;

public sealed class EmployeeApiClient : IEmployeeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeApiClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient("EmployeeDirectoryApi");
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<Employee>> GetEmployeesAsync(CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/employee");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Employee>>(cancellationToken)
            ?? throw new InvalidOperationException("The employee API returned no response body.");
    }

    public async Task AddEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/employee");
        request.Content = JsonContent.Create(employee);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateEmployeeAsync(
        Employee employee,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Put, $"api/employee/{employee.Id}");
        request.Content = JsonContent.Create(employee);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteEmployeeAsync(int id, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/employee/{id}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var apiCookie = _httpContextAccessor.HttpContext?.User
            .FindFirst(AppClaimTypes.ApiCookie)?.Value
            ?? throw new InvalidOperationException("The signed-in user has no API authentication cookie.");

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("Cookie", apiCookie);
        return request;
    }
}
