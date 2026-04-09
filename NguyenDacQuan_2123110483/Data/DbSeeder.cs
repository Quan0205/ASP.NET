using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Data;

public static class DbSeeder
{
    private sealed record PermissionSeed(string Code, string Name, string Description);
    private sealed record SystemRoleSeed(string Code, string Name, string Description, string[] PermissionCodes);

    private static readonly PermissionSeed[] PermissionSeeds =
    [
        new(PermissionCodes.DashboardView, "Xem dashboard", "Truy cập trang tổng quan."),
        new(PermissionCodes.MasterBranchesManage, "Quản lý chi nhánh", "CRUD chi nhánh."),
        new(PermissionCodes.MasterPositionsManage, "Quản lý chức vụ", "CRUD chức vụ nghiệp vụ."),
        new(PermissionCodes.MasterShiftsManage, "Quản lý ca làm", "CRUD ca làm."),
        new(PermissionCodes.HrEmployeesManage, "Quản lý nhân viên", "CRUD hồ sơ nhân viên."),
        new(PermissionCodes.HrContractsManage, "Quản lý hợp đồng", "CRUD hợp đồng lao động."),
        new(PermissionCodes.SecurityAccountsManage, "Quản lý tài khoản", "CRUD tài khoản người dùng."),
        new(PermissionCodes.OpsSchedulesManage, "Quản lý lịch làm", "CRUD và kiểm tra lịch làm."),
        new(PermissionCodes.OpsAttendanceManage, "Quản lý chấm công", "Check-in, check-out và xem công."),
        new(PermissionCodes.PayrollManage, "Quản lý payroll", "CRUD và generate payroll."),
        new(PermissionCodes.RecruitmentManage, "Quản lý tuyển dụng", "CRUD đợt tuyển dụng và ứng viên."),
        new(PermissionCodes.TrainingManage, "Quản lý đào tạo", "CRUD đào tạo và đào tạo nhân viên."),
        new(PermissionCodes.KpiManage, "Quản lý KPI", "CRUD KPI theo kỳ."),
        new(PermissionCodes.AuditView, "Xem audit log", "Tra cứu log hệ thống."),
        new(PermissionCodes.SelfAttendance, "Tự chấm công", "Dùng cho nhân viên tự check-in/check-out."),
        new(PermissionCodes.SelfPayrollView, "Xem payroll cá nhân", "Dùng cho nhân viên xem lương của mình."),
        new(PermissionCodes.ProfileView, "Xem hồ sơ tài khoản", "Xem thông tin tài khoản hiện tại."),
        new(PermissionCodes.LeaveManage, "Quản lý nghỉ phép", "Tạo, duyệt và theo dõi đơn nghỉ phép."),
        new(PermissionCodes.ShiftSwapManage, "Quản lý đổi ca", "Tạo và duyệt yêu cầu đổi ca."),
        new(PermissionCodes.AttendanceAdjustmentManage, "Điều chỉnh chấm công", "Tạo và duyệt yêu cầu điều chỉnh công."),
        new(PermissionCodes.ReportsView, "Xem báo cáo", "Xem dashboard và báo cáo tổng hợp."),
        new(PermissionCodes.ReportsExport, "Xuất báo cáo", "Xuất dữ liệu CSV/PDF từ báo cáo."),
        new(PermissionCodes.OperationsManage, "Quản trị vận hành", "Đóng kỳ lương, đánh dấu absent và thao tác vận hành.")
    ];

    private static readonly SystemRoleSeed[] SystemRoleSeeds =
    [
        new("ADMIN", "Administrator", "Toàn quyền hệ thống.", PermissionSeeds.Select(x => x.Code).ToArray()),
        new("HR", "HR Manager", "Quản lý nhân sự và dữ liệu hỗ trợ.", new[]
        {
            PermissionCodes.DashboardView,
            PermissionCodes.MasterBranchesManage,
            PermissionCodes.MasterPositionsManage,
            PermissionCodes.MasterShiftsManage,
            PermissionCodes.HrEmployeesManage,
            PermissionCodes.HrContractsManage,
            PermissionCodes.SecurityAccountsManage,
            PermissionCodes.OpsSchedulesManage,
            PermissionCodes.OpsAttendanceManage,
            PermissionCodes.PayrollManage,
            PermissionCodes.RecruitmentManage,
            PermissionCodes.TrainingManage,
            PermissionCodes.KpiManage,
            PermissionCodes.AuditView,
            PermissionCodes.ProfileView,
            PermissionCodes.LeaveManage,
            PermissionCodes.ShiftSwapManage,
            PermissionCodes.AttendanceAdjustmentManage,
            PermissionCodes.ReportsView,
            PermissionCodes.ReportsExport,
            PermissionCodes.OperationsManage
        }),
        new("MANAGER", "Store Manager", "Quản lý vận hành chi nhánh.", new[]
        {
            PermissionCodes.DashboardView,
            PermissionCodes.OpsSchedulesManage,
            PermissionCodes.OpsAttendanceManage,
            PermissionCodes.PayrollManage,
            PermissionCodes.SelfPayrollView,
            PermissionCodes.SelfAttendance,
            PermissionCodes.ProfileView,
            PermissionCodes.LeaveManage,
            PermissionCodes.ShiftSwapManage,
            PermissionCodes.AttendanceAdjustmentManage,
            PermissionCodes.ReportsView
        }),
        new("EMPLOYEE", "Employee", "Nhân viên sử dụng chức năng tự phục vụ.", new[]
        {
            PermissionCodes.DashboardView,
            PermissionCodes.SelfAttendance,
            PermissionCodes.SelfPayrollView,
            PermissionCodes.ProfileView
        })
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsurePermissionsAsync(db, cancellationToken);
        await EnsureSystemRolesAsync(db, cancellationToken);
        await EnsureDefaultAdminAsync(db, cancellationToken);
        await AssignFallbackSystemRolesAsync(db, cancellationToken);
    }

    private static async Task EnsurePermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        foreach (var seed in PermissionSeeds)
        {
            var permission = await db.Permissions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);

            if (permission == null)
            {
                db.Permissions.Add(new Permission
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    Description = seed.Description
                });
            }
            else
            {
                permission.Name = seed.Name;
                permission.Description = seed.Description;
                permission.IsDeleted = false;
                permission.DeletedAt = null;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSystemRolesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions
            .IgnoreQueryFilters()
            .ToDictionaryAsync(x => x.Code, x => x, cancellationToken);

        foreach (var seed in SystemRoleSeeds)
        {
            var role = await db.SystemRoles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);

            if (role == null)
            {
                role = new SystemRole
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    Description = seed.Description
                };
                db.SystemRoles.Add(role);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                role.Name = seed.Name;
                role.Description = seed.Description;
                role.IsActive = true;
                role.IsDeleted = false;
                role.DeletedAt = null;
                await db.SaveChangesAsync(cancellationToken);
            }

            var existingPermissionIds = await db.SystemRolePermissions
                .Where(x => x.SystemRoleId == role.Id)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);

            foreach (var permissionCode in seed.PermissionCodes)
            {
                if (!permissions.TryGetValue(permissionCode, out var permission) || existingPermissionIds.Contains(permission.Id))
                {
                    continue;
                }

                db.SystemRolePermissions.Add(new SystemRolePermission
                {
                    SystemRoleId = role.Id,
                    PermissionId = permission.Id
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureDefaultAdminAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var branch = await db.Branches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.BranchCode == "BR-HEAD", cancellationToken);

        if (branch == null)
        {
            branch = new Branch
            {
                BranchCode = "BR-HEAD",
                BranchName = "Head Office",
                Address = "Head Office",
                IsActive = true
            };
            db.Branches.Add(branch);
        }
        else
        {
            branch.BranchName = "Head Office";
            branch.Address = "Head Office";
            branch.IsActive = true;
            branch.IsDeleted = false;
            branch.DeletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        var jobRole = await db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.RoleName == "System Admin", cancellationToken);

        if (jobRole == null)
        {
            jobRole = new Role
            {
                RoleName = "System Admin",
                Description = "Default job role for the seeded admin account.",
                IsActive = true
            };
            db.Roles.Add(jobRole);
        }
        else
        {
            jobRole.Description = "Default job role for the seeded admin account.";
            jobRole.IsActive = true;
            jobRole.IsDeleted = false;
            jobRole.DeletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        var employee = await db.Employees
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.EmployeeCode == "EMP-ADMIN", cancellationToken);

        if (employee == null)
        {
            employee = new Employee
            {
                EmployeeCode = "EMP-ADMIN",
                FullName = "System Administrator",
                Gender = GenderType.Other,
                BranchId = branch.Id,
                RoleId = jobRole.Id,
                HireDate = DateTime.UtcNow,
                IsActive = true
            };
            db.Employees.Add(employee);
        }
        else
        {
            employee.FullName = "System Administrator";
            employee.Gender = GenderType.Other;
            employee.BranchId = branch.Id;
            employee.RoleId = jobRole.Id;
            employee.HireDate = employee.HireDate == default ? DateTime.UtcNow : employee.HireDate;
            employee.IsActive = true;
            employee.IsDeleted = false;
            employee.DeletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);

        var systemRole = await db.SystemRoles.FirstAsync(x => x.Code == "ADMIN", cancellationToken);
        var adminAccount = await db.UserAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Username == "admin", cancellationToken);

        if (adminAccount == null)
        {
            db.UserAccounts.Add(new UserAccount
            {
                EmployeeId = employee.Id,
                RoleId = jobRole.Id,
                SystemRoleId = systemRole.Id,
                Username = "admin",
                PasswordHash = PasswordHashHelper.Hash("admin123"),
                IsActive = true
            });
        }
        else
        {
            adminAccount.EmployeeId = employee.Id;
            adminAccount.RoleId = jobRole.Id;
            adminAccount.SystemRoleId = systemRole.Id;
            adminAccount.PasswordHash = PasswordHashHelper.Hash("admin123");
            adminAccount.IsActive = true;
            adminAccount.IsDeleted = false;
            adminAccount.DeletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task AssignFallbackSystemRolesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var systemRoles = await db.SystemRoles.ToDictionaryAsync(x => x.Code, x => x, cancellationToken);
        var accounts = await db.UserAccounts
            .Include(x => x.Role)
            .Where(x => x.SystemRoleId == null)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return;
        }

        foreach (var account in accounts)
        {
            var systemRoleCode = InferSystemRoleCode(account.Role?.RoleName);
            if (!systemRoles.TryGetValue(systemRoleCode, out var systemRole))
            {
                continue;
            }

            account.SystemRoleId = systemRole.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string InferSystemRoleCode(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return "EMPLOYEE";
        }

        var normalized = roleName.Trim().ToLowerInvariant();
        if (normalized.Contains("admin") || normalized.Contains("quản trị") || normalized.Contains("quan tri"))
        {
            return "ADMIN";
        }

        if (normalized.Contains("hr") || normalized.Contains("nhân sự") || normalized.Contains("nhan su") || normalized.Contains("tuyển"))
        {
            return "HR";
        }

        if (normalized.Contains("manager") || normalized.Contains("quản lý") || normalized.Contains("quan ly"))
        {
            return "MANAGER";
        }

        return "EMPLOYEE";
    }
}
