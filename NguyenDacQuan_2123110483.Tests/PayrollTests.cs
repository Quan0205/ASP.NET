using CoffeeHRM.Controllers;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Tests;

public class PayrollTests
{
    [Fact]
    public async Task DeletePayroll_ShouldRejectApprovedPayroll()
    {
        await using var context = TestDbFactory.CreateContext(nameof(DeletePayroll_ShouldRejectApprovedPayroll));

        var branch = new Branch { BranchCode = "B1", BranchName = "Main", Address = "Main", IsActive = true };
        var role = new Role { RoleName = "Cashier", IsActive = true };
        var employee = new Employee { EmployeeCode = "E1", FullName = "Employee", Branch = branch, Role = role, IsActive = true };
        var contract = new EmployeeContract { ContractNo = "C1", Employee = employee, ContractType = ContractType.FullTime, StartDate = DateTime.Today, BaseSalary = 10000000, HourlyRate = 50000, StandardDailyHours = 8, IsActive = true };
        var payroll = new Payroll { Employee = employee, EmployeeContract = contract, PayrollMonth = 4, PayrollYear = 2026, Status = PayrollStatus.Approved };
        context.AddRange(branch, role, employee, contract, payroll);
        await context.SaveChangesAsync();

        var controller = new PayrollController(new PayrollService(context, new FakeCurrentUserService()));
        var result = await controller.DeletePayroll(payroll.Id, default);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
        Assert.Contains("draft", conflict.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
