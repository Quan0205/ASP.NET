using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<KPI> KPIs => Set<KPI>();
    public DbSet<Recruitment> Recruitments => Set<Recruitment>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
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
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.BaseAmount).HasPrecision(18, 2);
            entity.Property(x => x.WorkingHours).HasPrecision(18, 2);
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
            entity.Property(x => x.OvertimeAmount).HasPrecision(18, 2);
            entity.Property(x => x.AllowanceAmount).HasPrecision(18, 2);
            entity.Property(x => x.BonusAmount).HasPrecision(18, 2);
            entity.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalSalary).HasPrecision(18, 2);

            entity.HasOne(x => x.Employee)
                .WithMany(x => x.Payrolls)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EmployeeContract)
                .WithMany(x => x.Payrolls)
                .HasForeignKey(x => x.EmployeeContractId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
