using CoffeeHRM.Controllers;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;

namespace CoffeeHRM.Tests;

public class ScheduleValidationTests
{
    [Fact]
    public async Task ValidateSchedule_ShouldRejectDuplicateEmployeeDate()
    {
        await using var context = TestDbFactory.CreateContext(nameof(ValidateSchedule_ShouldRejectDuplicateEmployeeDate));

        var branch = new Branch { BranchCode = "B1", BranchName = "Main", Address = "Main", IsActive = true };
        var role = new Role { RoleName = "Cashier", IsActive = true };
        var employee = new Employee { EmployeeCode = "E1", FullName = "Employee", Branch = branch, Role = role, IsActive = true };
        var shift = new Shift { ShiftCode = "S1", ShiftName = "Morning", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), IsActive = true };
        context.AddRange(branch, role, employee, shift);
        await context.SaveChangesAsync();

        context.Schedules.Add(new Schedule { EmployeeId = employee.Id, ShiftId = shift.Id, ScheduleDate = new DateTime(2026, 4, 10) });
        await context.SaveChangesAsync();

        var controller = new SchedulesController(new ScheduleService(context));
        var result = await controller.ValidateSchedule(new ScheduleRequestDto(employee.Id, shift.Id, new DateTime(2026, 4, 10), null), default);
        var payload = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result).Value as ScheduleValidationResultDto;

        Assert.NotNull(payload);
        Assert.False(payload.IsValid);
    }
}
