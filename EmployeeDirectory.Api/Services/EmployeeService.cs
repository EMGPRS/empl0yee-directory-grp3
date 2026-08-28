namespace EmployeeDirectory.Api.Services
{
    public class EmployeeService : IEmployeeService
    {

        private readonly List<Employee> _employees = new List<Employee>
        {
            new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Department = "HR", Gender = Gender.Male },
            new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Department = "IT", Gender = Gender.Female },
            new Employee { Id = 3, FirstName = "Michael", LastName = "Johnson", Email = "michael.johnson@example.com", Department = "Finance", Gender = Gender.Male }
        };
        public async Task<Employee> GetEmployeeAsync(int Id)
        {
            return await Task.FromResult(_employees.FirstOrDefault(e => e.Id == Id));
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            return await Task.FromResult(_employees);
        }
    }
}
