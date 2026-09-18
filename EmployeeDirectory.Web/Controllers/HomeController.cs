using EmployeeDirectory.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EmployeeDirectory.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("EmployeeDirectoryApi");
        }
        public async Task<IActionResult> Index(int? id)
        {
            var employees = await _httpClient.GetFromJsonAsync<List<Employee>>("api/employee");
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
        public async Task<IActionResult> Save(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            if(employee.Id == 0)
            {
                await _httpClient.PostAsJsonAsync("api/employee", employee);
            }
            else
            {
                await _httpClient.PutAsJsonAsync($"api/employee/{employee.Id}", employee);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _httpClient.DeleteAsync($"api/employee/{id}");
            return RedirectToAction("Index");
        }
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
