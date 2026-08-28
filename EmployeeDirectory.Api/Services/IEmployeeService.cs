namespace EmployeeDirectory.Api.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetEmployeesAsync();  
        Task<Employee> GetEmployeeAsync(int Id);  
    }
}
