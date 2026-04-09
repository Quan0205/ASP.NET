using System.Reflection;
using System.Text.Json;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CoffeeHRM.Data;

public class AppDbContext : DbContext
{
    private static readonly HashSet<Type> AuditIgnoredTypes =
    [
        typeof(AuditLog),
        typeof(RefreshToken)
    ];

    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<SystemRole> SystemRoles => Set<SystemRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<SystemRolePermission> SystemRolePermissions => Set<SystemRolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollClosePeriod> PayrollClosePeriods => Set<PayrollClosePeriod>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<KPI> KPIs => Set<KPI>();
    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<AttendanceAdjustment> AttendanceAdjustments => Set<AttendanceAdjustment>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges()
    {
        return SaveChangesWithAuditAsync().GetAwaiter().GetResult();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesWithAuditAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(x => x.BranchCode).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(x => x.RoleName).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<SystemRole>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<SystemRolePermission>(entity =>
        {
            entity.HasKey(x => new { x.SystemRoleId, x.PermissionId });
            entity.HasQueryFilter(x => x.SystemRole != null && !x.SystemRole.IsDeleted && x.Permission != null && !x.Permission.IsDeleted);

            entity.HasOne(x => x.SystemRole)
                .WithMany(x => x.SystemRolePermissions)
                .HasForeignKey(x => x.SystemRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.SystemRolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserAccountId, x.ExpiresAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.Property(x => x.CreatedByIp).HasMaxLength(50);
            entity.Property(x => x.RevokedByIp).HasMaxLength(50);

            entity.HasOne(x => x.UserAccount)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(x => x.EmployeeCode).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.Property(x => x.Gender).HasConversion<int>();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.HireDate).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeContract>(entity =>
        {
            entity.HasIndex(x => x.ContractNo).IsUnique();
            entity.HasIndex(x => new { x.EmployeeId, x.IsActive });
            entity.Property(x => x.ContractType).HasConversion<int>();
            entity.Property(x => x.BaseSalary).HasPrecision(18, 2);
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
            entity.Property(x => x.OvertimeRateMultiplier).HasPrecision(18, 2).HasDefaultValue(1.50m);
            entity.Property(x => x.LatePenaltyPerMinute).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.EarlyLeavePenaltyPerMinute).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(x => x.StandardDailyHours).HasPrecision(5, 2).HasDefaultValue(8m);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.EmployeeContracts)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasIndex(x => x.ShiftCode).IsUnique();
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.Property(x => x.GraceMinutes).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.ScheduleDate }).IsUnique();
            entity.HasIndex(x => new { x.ShiftId, x.ScheduleDate });
            entity.Property(x => x.ScheduleDate).HasColumnType("date");

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Shift)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.AttendanceDate }).IsUnique();
            entity.HasIndex(x => x.ScheduleId).IsUnique().HasFilter("[ScheduleId] IS NOT NULL");
            entity.Property(x => x.AttendanceDate).HasColumnType("date");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.LateMinutes).HasDefaultValue(0);
            entity.Property(x => x.WorkingMinutes).HasDefaultValue(0);
            entity.Property(x => x.OvertimeMinutes).HasDefaultValue(0);
            entity.Property(x => x.EarlyLeaveMinutes).HasDefaultValue(0);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Shift)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Schedule)
                .WithOne(x => x.Attendance)
                .HasForeignKey<Attendance>(x => x.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.PayrollMonth, x.PayrollYear }).IsUnique();
            entity.HasIndex(x => x.EmployeeContractId);
            entity.HasIndex(x => new { x.PayrollMonth, x.PayrollYear, x.IsClosed });
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.BaseAmount).HasPrecision(18, 2);
            entity.Property(x => x.WorkingHours).HasPrecision(18, 2);
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
            entity.Property(x => x.OvertimeAmount).HasPrecision(18, 2);
            entity.Property(x => x.AllowanceAmount).HasPrecision(18, 2);
            entity.Property(x => x.BonusAmount).HasPrecision(18, 2);
            entity.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
            entity.Property(x => x.InsuranceAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalSalary).HasPrecision(18, 2);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Payrolls)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EmployeeContract)
                .WithMany(x => x.Payrolls)
                .HasForeignKey(x => x.EmployeeContractId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ApprovedByUserAccount)
                .WithMany(x => x.ApprovedPayrolls)
                .HasForeignKey(x => x.ApprovedByUserAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ClosedByUserAccount)
                .WithMany(x => x.ClosedPayrolls)
                .HasForeignKey(x => x.ClosedByUserAccountId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PayrollClosePeriod>(entity =>
        {
            entity.HasIndex(x => new { x.PayrollMonth, x.PayrollYear }).IsUnique();

            entity.HasOne(x => x.ClosedByUserAccount)
                .WithMany()
                .HasForeignKey(x => x.ClosedByUserAccountId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PayrollDetail>(entity =>
        {
            entity.HasIndex(x => x.PayrollId);
            entity.HasIndex(x => x.AttendanceId);
            entity.HasIndex(x => new { x.PayrollId, x.DetailType });
            entity.Property(x => x.DetailType).HasConversion<int>();
            entity.Property(x => x.Amount).HasPrecision(18, 2);

            entity.HasOne(x => x.Payroll)
                .WithMany(x => x.PayrollDetails)
                .HasForeignKey(x => x.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Attendance)
                .WithMany()
                .HasForeignKey(x => x.AttendanceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Schedule)
                .WithMany()
                .HasForeignKey(x => x.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<KPI>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.KpiMonth, x.KpiYear }).IsUnique();
            entity.Property(x => x.Score).HasPrecision(5, 2);
            entity.Property(x => x.Target).HasPrecision(5, 2);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.KPIs)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recruitment>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Status);

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Recruitments)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.InterviewScore).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.RecruitmentId, x.Email }).IsUnique().HasFilter("[Email] IS NOT NULL");

            entity.HasOne(x => x.Recruitment)
                .WithMany(x => x.Candidates)
                .HasForeignKey(x => x.RecruitmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Training>(entity =>
        {
            entity.HasIndex(x => x.TrainingCode).IsUnique();
            entity.Property(x => x.IsRequired).HasDefaultValue(false);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<EmployeeTraining>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.TrainingId }).IsUnique();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Score).HasPrecision(5, 2);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.EmployeeTrainings)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Training)
                .WithMany(x => x.EmployeeTrainings)
                .HasForeignKey(x => x.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
            entity.Property(x => x.StartDate).HasColumnType("date");
            entity.Property(x => x.EndDate).HasColumnType("date");
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.TotalDays).HasPrecision(5, 2);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.LeaveRequests)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByUserAccount)
                .WithMany(x => x.ReviewedLeaveRequests)
                .HasForeignKey(x => x.ReviewedByUserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ShiftSwapRequest>(entity =>
        {
            entity.HasIndex(x => new { x.RequestScheduleId, x.TargetScheduleId });
            entity.Property(x => x.Status).HasConversion<int>();

            entity.HasOne(x => x.RequestEmployee)
                .WithMany(x => x.RequestedShiftSwaps)
                .HasForeignKey(x => x.RequestEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TargetEmployee)
                .WithMany(x => x.TargetShiftSwaps)
                .HasForeignKey(x => x.TargetEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RequestSchedule)
                .WithMany(x => x.RequestedShiftSwaps)
                .HasForeignKey(x => x.RequestScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TargetSchedule)
                .WithMany(x => x.TargetShiftSwaps)
                .HasForeignKey(x => x.TargetScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByUserAccount)
                .WithMany(x => x.ReviewedShiftSwapRequests)
                .HasForeignKey(x => x.ReviewedByUserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AttendanceAdjustment>(entity =>
        {
            entity.HasIndex(x => new { x.AttendanceId, x.Status });
            entity.Property(x => x.RequestedStatus).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();

            entity.HasOne(x => x.Attendance)
                .WithMany()
                .HasForeignKey(x => x.AttendanceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.AttendanceAdjustments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByUserAccount)
                .WithMany(x => x.ReviewedAttendanceAdjustments)
                .HasForeignKey(x => x.ReviewedByUserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Employee)
                .WithOne(x => x.UserAccount)
                .HasForeignKey<UserAccount>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserAccounts)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SystemRole)
                .WithMany(x => x.UserAccounts)
                .HasForeignKey(x => x.SystemRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => new { x.TableName, x.RecordId });
            entity.HasIndex(x => x.UserAccountId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.UserAccount)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        ApplySoftDeleteFilters(modelBuilder);
    }

    private async Task<int> SaveChangesWithAuditAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingAudits = CapturePendingAudits(now);
        ApplyAuditMetadata(now);

        if (pendingAudits.Count == 0)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        if (Database.CurrentTransaction != null)
        {
            var writtenInsideTransaction = await base.SaveChangesAsync(cancellationToken);
            AuditLogs.AddRange(pendingAudits.Select(audit => audit.ToAuditLog(now, _currentUserService)));
            await base.SaveChangesAsync(cancellationToken);
            return writtenInsideTransaction;
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        var written = await base.SaveChangesAsync(cancellationToken);

        AuditLogs.AddRange(pendingAudits.Select(audit => audit.ToAuditLog(now, _currentUserService)));
        await base.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return written;
    }

    private List<PendingAudit> CapturePendingAudits(DateTime now)
    {
        var audits = new List<PendingAudit>();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.Entity is AuditLog || AuditIgnoredTypes.Contains(entry.Entity.GetType()))
            {
                continue;
            }

            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            var recordIdGetter = BuildRecordIdGetter(entry.Entity);

            switch (entry.State)
            {
                case EntityState.Added:
                    audits.Add(new PendingAudit(
                        "Create",
                        tableName,
                        recordIdGetter,
                        SerializeValues(entry.CurrentValues, excludeNulls: true),
                        null,
                        false));
                    break;
                case EntityState.Modified:
                    audits.Add(new PendingAudit(
                        "Update",
                        tableName,
                        recordIdGetter,
                        SerializeValues(entry.CurrentValues, excludeNulls: true),
                        SerializeValues(entry.OriginalValues, excludeNulls: true),
                        false));
                    break;
                case EntityState.Deleted:
                    audits.Add(new PendingAudit(
                        "SoftDelete",
                        tableName,
                        recordIdGetter,
                        JsonSerializer.Serialize(new
                        {
                            IsDeleted = true,
                            DeletedAt = now
                        }),
                        SerializeValues(entry.OriginalValues, excludeNulls: true),
                        true));
                    break;
            }
        }

        return audits;
    }

    private void ApplyAuditMetadata(DateTime now)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.DeletedAt = null;
                entry.Entity.IsDeleted = false;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private static string SerializeValues(PropertyValues values, bool excludeNulls)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in values.Properties)
        {
            if (property.IsShadowProperty())
            {
                continue;
            }

            if (property.Name is nameof(AuditableEntity.CreatedAt) or nameof(AuditableEntity.UpdatedAt) or nameof(AuditableEntity.DeletedAt) or nameof(AuditableEntity.IsDeleted))
            {
                continue;
            }

            if (property.Name == "Id")
            {
                continue;
            }

            var value = values[property];
            if (excludeNulls && value is null)
            {
                continue;
            }

            data[property.Name] = value;
        }

        return JsonSerializer.Serialize(data);
    }

    private static Func<string> BuildRecordIdGetter(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty == null)
        {
            return () => string.Empty;
        }

        return () => Convert.ToString(idProperty.GetValue(entity)) ?? string.Empty;
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        var auditableEntities = modelBuilder.Model.GetEntityTypes()
            .Select(x => x.ClrType)
            .Where(type => typeof(AuditableEntity).IsAssignableFrom(type) && type != typeof(AuditLog))
            .Distinct()
            .ToList();

        foreach (var type in auditableEntities)
        {
            var method = typeof(AppDbContext)
                .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type);

            method.Invoke(null, [modelBuilder]);
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(x => !x.IsDeleted);
    }

    private sealed record PendingAudit(
        string Action,
        string TableName,
        Func<string> RecordIdGetter,
        string? NewValues,
        string? OldValues,
        bool IsSoftDelete)
    {
        public AuditLog ToAuditLog(DateTime now, ICurrentUserService currentUserService)
        {
            return new AuditLog
            {
                Action = Action,
                TableName = TableName,
                RecordId = RecordIdGetter(),
                OldValues = OldValues,
                NewValues = NewValues,
                UserAccountId = currentUserService.UserId,
                IpAddress = currentUserService.IpAddress,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }
}
