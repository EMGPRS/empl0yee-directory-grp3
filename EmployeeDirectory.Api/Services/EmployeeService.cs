using EmployeeDirectory.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeDirectory.Api.Services
{
    public class EmployeeService : IEmployeeService
    {
        private EmployeeDBContext _context;

        public EmployeeService(EmployeeDBContext context)
        {
            _context = context;
        }

        public async Task<Employee> AddEmployeeAsync(Employee employee)
        {
            employee.Id = 0;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(); //commit to the db
            return employee;
        }

        public  async Task<bool> DeleteEmployeeAsync(int Id)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.Id == Id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public  Task<Employee> GetEmployeeAsync(int Id)
        {
            return _context.Employees.FirstOrDefaultAsync(x => x.Id == Id);
        }

        public  Task<List<Employee>> GetEmployeesAsync()
        {
            return _context.Employees.OrderBy(x => x.FirstName).ToListAsync();
        }

        public async Task<Employee> UpdateEmployeeAsync(int Id, Employee employee)
        {
            var current = _context.Employees.FirstOrDefault(e => e.Id == Id);
            if (current != null)
            {
                current.FirstName = employee.FirstName;
                current.LastName = employee.LastName;
                current.Email = employee.Email;
                current.Department = employee.Department;
                current.Gender = employee.Gender;
                await _context.SaveChangesAsync();
                return current;
            }
            return null;
        }
    }
}
