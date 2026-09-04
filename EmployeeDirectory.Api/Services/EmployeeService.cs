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

        public Task<Employee> AddEmployeeAsync(Employee employee)
        {
            employee.Id = _employees.Max(e => e.Id) + 1;
            _employees.Add(employee);
            return Task.FromResult(employee);
        }

        public Task<bool> DeleteEmployeeAsync(int Id)
        {
            var employee = _employees.FirstOrDefault(e => e.Id == Id);
            if (employee != null)
            {
                _employees.Remove(employee);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task<Employee> GetEmployeeAsync(int Id)
        {
            return await Task.FromResult(_employees.FirstOrDefault(e => e.Id == Id));
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            return await Task.FromResult(_employees);
        }

        public Task<Employee> UpdateEmployeeAsync(int Id, Employee employee)
        {
            var current = _employees.FirstOrDefault(e => e.Id == Id);
            if (current != null)
            {
                current.FirstName = employee.FirstName;
                current.LastName = employee.LastName;
                current.Email = employee.Email;
                current.Department = employee.Department;
                current.Gender = employee.Gender;
                return Task.FromResult(current);
            }
            return Task.FromResult<Employee>(null);
        }
    }
}
