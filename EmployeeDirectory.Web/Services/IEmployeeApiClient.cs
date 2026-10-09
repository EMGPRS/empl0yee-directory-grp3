using EmployeeDirectory.Web.Models;

namespace EmployeeDirectory.Web.Services;

public interface IEmployeeApiClient
{
    Task<List<Employee>> GetEmployeesAsync(CancellationToken cancellationToken);
    Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken);
    Task UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken);
    Task DeleteEmployeeAsync(int id, CancellationToken cancellationToken);
}
