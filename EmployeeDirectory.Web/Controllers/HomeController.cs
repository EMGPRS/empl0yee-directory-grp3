using EmployeeDirectory.Web.Authorization;
using EmployeeDirectory.Web.Models;
using EmployeeDirectory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EmployeeDirectory.Web.Controllers
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Viewer}")]
    public class HomeController : Controller
    {
        private readonly IEmployeeApiClient _employeeApiClient;

        public HomeController(IEmployeeApiClient employeeApiClient)
        {
            _employeeApiClient = employeeApiClient;
        }

        public async Task<IActionResult> Index(int? id, CancellationToken cancellationToken)
        {
            var employees = await _employeeApiClient.GetEmployeesAsync(cancellationToken);
            Employee employee = new Employee();
            if (id.HasValue)
            {
                employee = employees.FirstOrDefault(x => x.Id == id.Value) ?? new Employee();
            }
            var viewModel = new EmployeeViewModel
            {
                Employees = employees,
                Employee = employee

            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            Employee employee,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            if(employee.Id == 0)
            {
                await _employeeApiClient.AddEmployeeAsync(employee, cancellationToken);
            }
            else
            {
                await _employeeApiClient.UpdateEmployeeAsync(employee, cancellationToken);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _employeeApiClient.DeleteEmployeeAsync(id, cancellationToken);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
