using EmployeeDirectory.Api.Authorization;
using EmployeeDirectory.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeDirectory.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Viewer}")]
    public class EmployeeController : ControllerBase
    {
        private readonly Services.IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return Ok(employees);
        }
        //routing /path/end point name
        //example search?searchTerm=John
        [HttpGet("search")]
        public async Task<IActionResult> SearchEmployees(string searchTerm)
        {
            var employees = await _employeeService.GetEmployeesAsync();
            var result = employees.Where(e => e.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                          e.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                          e.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            if (result.Any())
            {
                return Ok(result);
            }
            return NotFound($"No results for {searchTerm}");
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> AddEmployee(Employee employee)
        {
            var result = await _employeeService.AddEmployeeAsync(employee);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var result = await _employeeService.GetEmployeeAsync(id);
            if (result != null)
                return Ok(result);
            return NotFound();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            var result = await _employeeService.UpdateEmployeeAsync(id, employee);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteEmplyee(int id)
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);
            if (result)
                return Accepted();
            return NotFound();
        }
    }
}
