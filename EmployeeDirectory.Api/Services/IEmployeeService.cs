namespace EmployeeDirectory.Api.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetEmployeesAsync();  
        Task<Employee> GetEmployeeAsync(int Id);
        Task<Employee> AddEmployeeAsync(Employee employee);
        Task<Employee> UpdateEmployeeAsync(int Id, Employee employee);
        Task<bool> DeleteEmployeeAsync(int Id);
    }
}
