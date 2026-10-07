using System;
using System.Collections.Generic;
using System.Text;
using EmployeeDirectory.Api.Services;
using EmployeeDirectory.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EmployeeDirectory.Api.Tests
{
    public class EmployeeControllerTests
    {
        // Add your test methods here
        [Fact]
        public async Task GetEmployees_ReturnsOkWithEmployees()
        {
            //Arrange
            var employees = new List<Employee> { new Employee { Id = 1, FirstName = "John", LastName = " Doe" } };
            //create a fake object
            var service = new Mock<IEmployeeService>();
            service.Setup(x => x.GetEmployeesAsync()).ReturnsAsync(employees);
            var controller = new EmployeeController(service.Object);

            //Act
            var result = await controller.GetEmployees();

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<Employee>>(okResult.Value);
            service.Verify(x => x.GetEmployeesAsync(), Times.Once);
            Assert.Same(employees, okResult.Value);

        }

        [Fact]
        public async Task SearchEmployees_ReturnOk_WithFilteredEmployees()
        {
            var employees = new List<Employee> 
            {
                new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email ="john@gmail.com" },
                new Employee { Id = 2, FirstName = "Mary", LastName = "Smith" , Email ="mary@gmail.com"},
            };
            var service = new Mock<IEmployeeService>();
            service.Setup(x => x.GetEmployeesAsync()).ReturnsAsync(employees);
            var controller = new EmployeeController(service.Object);

            //Act
            var result = await controller.SearchEmployees("John");

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedData = Assert.IsType<List<Employee>>(okResult.Value);
            Assert.Equal("John", returnedData[0].FirstName);

        }

        [Fact]
        public async Task SearchEmployees_ReturnNotFound_WithNoEmployees()
        {
            var employees = new List<Employee>
            {
                new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email ="john@gmail.com" },
                new Employee { Id = 2, FirstName = "Mary", LastName = "Smith" , Email ="mary@gmail.com"},
            };
            var service = new Mock<IEmployeeService>();
            service.Setup(x => x.GetEmployeesAsync()).ReturnsAsync(employees);
            var controller = new EmployeeController(service.Object);

            //Act
            var result = await controller.SearchEmployees("David");

            //Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No results for David", notFoundResult.Value);

        }

        [Fact]
        public async Task AddEmployee_ReturnOk()
        {

            var employee = new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@gmail.com" };
           
            var service = new Mock<IEmployeeService>();
            service.Setup(x => x.AddEmployeeAsync(It.IsAny<Employee>())).ReturnsAsync(employee);
            var controller = new EmployeeController(service.Object);

            //Act
            var result = await controller.AddEmployee(employee);

            //Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(employee, okResult.Value);

        }
    }
}
