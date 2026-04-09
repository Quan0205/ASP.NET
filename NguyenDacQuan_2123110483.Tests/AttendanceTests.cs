using CoffeeHRM.Controllers;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using CoffeeHRM.Services;

namespace CoffeeHRM.Tests;

public class AttendanceTests
{
    [Fact]
    public async Task CheckIn_ShouldFail_WhenEmployeeIsInactive()
    {
        await using var context = TestDbFactory.CreateContext(nameof(CheckIn_ShouldFail_WhenEmployeeIsInactive));

        var branch = new Branch { BranchCode = "B1", BranchName = "Main", Address = "Main", IsActive = true };
        var role = new Role { RoleName = "Cashier", IsActive = true };
        var employee = new Employee { EmployeeCode = "E1", FullName = "Employee", Branch = branch, Role = role, IsActive = false };
        context.AddRange(branch, role, employee);
        await context.SaveChangesAsync();

        var controller = new AttendanceController(new AttendanceService(context));
        var result = await controller.CheckIn(new CheckInRequestDto(employee.Id, new DateTime(2026, 4, 10, 8, 0, 0), null), default);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}
