IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Branches] (
    [Id] int NOT NULL IDENTITY,
    [BranchCode] nvarchar(20) NOT NULL,
    [BranchName] nvarchar(150) NOT NULL,
    [Address] nvarchar(250) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [RoleName] nvarchar(100) NOT NULL,
    [Description] nvarchar(250) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Shifts] (
    [Id] int NOT NULL IDENTITY,
    [ShiftCode] nvarchar(20) NOT NULL,
    [ShiftName] nvarchar(100) NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [GraceMinutes] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Trainings] (
    [Id] int NOT NULL IDENTITY,
    [TrainingCode] nvarchar(20) NOT NULL,
    [TrainingName] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [Instructor] nvarchar(100) NULL,
    [IsRequired] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Trainings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Recruitments] (
    [Id] int NOT NULL IDENTITY,
    [BranchId] int NULL,
    [PositionTitle] nvarchar(150) NOT NULL,
    [OpenDate] datetime2 NOT NULL,
    [CloseDate] datetime2 NULL,
    [Status] nvarchar(30) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Recruitments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Recruitments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeCode] nvarchar(20) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [DateOfBirth] datetime2 NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(150) NULL,
    [Address] nvarchar(250) NULL,
    [BranchId] int NOT NULL,
    [RoleId] int NOT NULL,
    [HireDate] datetime2 NOT NULL DEFAULT (GETDATE()),
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Candidates] (
    [Id] int NOT NULL IDENTITY,
    [RecruitmentId] int NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(150) NULL,
    [AppliedDate] datetime2 NOT NULL DEFAULT (GETDATE()),
    [Status] nvarchar(30) NOT NULL,
    [InterviewScore] decimal(5,2) NULL,
    [Note] nvarchar(250) NULL,
    CONSTRAINT [PK_Candidates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Candidates_Recruitments_RecruitmentId] FOREIGN KEY ([RecruitmentId]) REFERENCES [Recruitments] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [EmployeeContracts] (
    [Id] int NOT NULL IDENTITY,
    [ContractNo] nvarchar(30) NOT NULL,
    [ContractType] nvarchar(50) NOT NULL,
    [EmployeeId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_EmployeeContracts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeContracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [EmployeeTrainings] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [TrainingId] int NOT NULL,
    [AssignedDate] datetime2 NOT NULL DEFAULT (GETDATE()),
    [CompletedDate] datetime2 NULL,
    [Status] nvarchar(30) NOT NULL,
    [Score] decimal(5,2) NULL,
    CONSTRAINT [PK_EmployeeTrainings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeTrainings_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeTrainings_Trainings_TrainingId] FOREIGN KEY ([TrainingId]) REFERENCES [Trainings] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [KPIs] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [KpiYear] int NOT NULL,
    [KpiMonth] int NOT NULL,
    [Score] decimal(5,2) NOT NULL,
    [Target] decimal(5,2) NOT NULL,
    [Result] nvarchar(100) NOT NULL,
    [Note] nvarchar(250) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_KPIs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_KPIs_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Payrolls] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [PayrollMonth] int NOT NULL,
    [PayrollYear] int NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [TotalAllowance] decimal(18,2) NOT NULL,
    [TotalPenalty] decimal(18,2) NOT NULL,
    [TotalSalary] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [PaidDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Payrolls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payrolls_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Schedules] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ShiftId] int NOT NULL,
    [ScheduleDate] date NOT NULL,
    [Note] nvarchar(250) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Schedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Schedules_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Schedules_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [UserAccounts] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [RoleId] int NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(250) NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserAccounts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserAccounts_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Allowances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [PayrollId] int NULL,
    [AllowanceType] nvarchar(100) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Allowances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Allowances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Allowances_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PayrollDetails] (
    [Id] int NOT NULL IDENTITY,
    [PayrollId] int NOT NULL,
    [DetailType] nvarchar(50) NOT NULL,
    [ReferenceType] nvarchar(50) NULL,
    [ReferenceId] int NULL,
    [Description] nvarchar(250) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_PayrollDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollDetails_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Penalties] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [PayrollId] int NULL,
    [PenaltyType] nvarchar(100) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(250) NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Penalties] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Penalties_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Penalties_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Attendances] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ShiftId] int NULL,
    [ScheduleId] int NULL,
    [AttendanceDate] date NOT NULL,
    [CheckInAt] datetime2 NULL,
    [CheckOutAt] datetime2 NULL,
    [LateMinutes] int NOT NULL,
    [WorkingMinutes] int NOT NULL,
    [Note] nvarchar(250) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Schedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [Schedules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserAccountId] int NULL,
    [Action] nvarchar(100) NOT NULL,
    [TableName] nvarchar(100) NOT NULL,
    [RecordId] nvarchar(50) NOT NULL,
    [OldValues] nvarchar(2000) NULL,
    [NewValues] nvarchar(2000) NULL,
    [IpAddress] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_UserAccounts_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_Allowances_EmployeeId] ON [Allowances] ([EmployeeId]);
GO

CREATE INDEX [IX_Allowances_PayrollId] ON [Allowances] ([PayrollId]);
GO

CREATE UNIQUE INDEX [IX_Attendances_EmployeeId_AttendanceDate] ON [Attendances] ([EmployeeId], [AttendanceDate]);
GO

CREATE UNIQUE INDEX [IX_Attendances_ScheduleId] ON [Attendances] ([ScheduleId]) WHERE [ScheduleId] IS NOT NULL;
GO

CREATE INDEX [IX_Attendances_ShiftId] ON [Attendances] ([ShiftId]);
GO

CREATE INDEX [IX_AuditLogs_UserAccountId] ON [AuditLogs] ([UserAccountId]);
GO

CREATE UNIQUE INDEX [IX_Branches_BranchCode] ON [Branches] ([BranchCode]);
GO

CREATE INDEX [IX_Candidates_RecruitmentId] ON [Candidates] ([RecruitmentId]);
GO

CREATE UNIQUE INDEX [IX_EmployeeContracts_ContractNo] ON [EmployeeContracts] ([ContractNo]);
GO

CREATE INDEX [IX_EmployeeContracts_EmployeeId] ON [EmployeeContracts] ([EmployeeId]);
GO

CREATE INDEX [IX_Employees_BranchId] ON [Employees] ([BranchId]);
GO

CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]) WHERE [Email] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
GO

CREATE INDEX [IX_Employees_RoleId] ON [Employees] ([RoleId]);
GO

CREATE UNIQUE INDEX [IX_EmployeeTrainings_EmployeeId_TrainingId] ON [EmployeeTrainings] ([EmployeeId], [TrainingId]);
GO

CREATE INDEX [IX_EmployeeTrainings_TrainingId] ON [EmployeeTrainings] ([TrainingId]);
GO

CREATE UNIQUE INDEX [IX_KPIs_EmployeeId_KpiMonth_KpiYear] ON [KPIs] ([EmployeeId], [KpiMonth], [KpiYear]);
GO

CREATE INDEX [IX_PayrollDetails_PayrollId] ON [PayrollDetails] ([PayrollId]);
GO

CREATE UNIQUE INDEX [IX_Payrolls_EmployeeId_PayrollMonth_PayrollYear] ON [Payrolls] ([EmployeeId], [PayrollMonth], [PayrollYear]);
GO

CREATE INDEX [IX_Penalties_EmployeeId] ON [Penalties] ([EmployeeId]);
GO

CREATE INDEX [IX_Penalties_PayrollId] ON [Penalties] ([PayrollId]);
GO

CREATE INDEX [IX_Recruitments_BranchId] ON [Recruitments] ([BranchId]);
GO

CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
GO

CREATE UNIQUE INDEX [IX_Schedules_EmployeeId_ScheduleDate] ON [Schedules] ([EmployeeId], [ScheduleDate]);
GO

CREATE INDEX [IX_Schedules_ShiftId] ON [Schedules] ([ShiftId]);
GO

CREATE UNIQUE INDEX [IX_Shifts_ShiftCode] ON [Shifts] ([ShiftCode]);
GO

CREATE UNIQUE INDEX [IX_Trainings_TrainingCode] ON [Trainings] ([TrainingCode]);
GO

CREATE UNIQUE INDEX [IX_UserAccounts_EmployeeId] ON [UserAccounts] ([EmployeeId]);
GO

CREATE INDEX [IX_UserAccounts_RoleId] ON [UserAccounts] ([RoleId]);
GO

CREATE UNIQUE INDEX [IX_UserAccounts_Username] ON [UserAccounts] ([Username]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260319082211_InitialCreate', N'8.0.13');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [Allowances];
GO

DROP TABLE [Penalties];
GO

DROP INDEX [IX_Schedules_ShiftId] ON [Schedules];
GO

DROP INDEX [IX_EmployeeContracts_EmployeeId] ON [EmployeeContracts];
GO

DROP INDEX [IX_Candidates_RecruitmentId] ON [Candidates];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PayrollDetails]') AND [c].[name] = N'ReferenceType');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [PayrollDetails] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [PayrollDetails] DROP COLUMN [ReferenceType];
GO

EXEC sp_rename N'[Payrolls].[TotalPenalty]', N'WorkingHours', N'COLUMN';
GO

EXEC sp_rename N'[Payrolls].[TotalAllowance]', N'PenaltyAmount', N'COLUMN';
GO

EXEC sp_rename N'[Payrolls].[BaseSalary]', N'OvertimeAmount', N'COLUMN';
GO

EXEC sp_rename N'[PayrollDetails].[ReferenceId]', N'SourceReferenceId', N'COLUMN';
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserAccounts]') AND [c].[name] = N'IsActive');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [UserAccounts] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [UserAccounts] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserAccounts]') AND [c].[name] = N'CreatedAt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [UserAccounts] DROP CONSTRAINT [' + @var2 + '];');
GO

ALTER TABLE [UserAccounts] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trainings]') AND [c].[name] = N'IsRequired');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Trainings] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Trainings] ADD DEFAULT CAST(0 AS bit) FOR [IsRequired];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trainings]') AND [c].[name] = N'IsActive');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Trainings] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Trainings] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Trainings]') AND [c].[name] = N'CreatedAt');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Trainings] DROP CONSTRAINT [' + @var5 + '];');
GO

ALTER TABLE [Trainings] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Shifts]') AND [c].[name] = N'IsActive');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Shifts] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Shifts] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Shifts]') AND [c].[name] = N'GraceMinutes');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Shifts] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Shifts] ADD DEFAULT 0 FOR [GraceMinutes];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Shifts]') AND [c].[name] = N'CreatedAt');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Shifts] DROP CONSTRAINT [' + @var8 + '];');
GO

ALTER TABLE [Shifts] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Schedules]') AND [c].[name] = N'CreatedAt');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Schedules] DROP CONSTRAINT [' + @var9 + '];');
GO

ALTER TABLE [Schedules] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'IsActive');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Roles] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'CreatedAt');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT [' + @var11 + '];');
GO

ALTER TABLE [Roles] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Recruitments]') AND [c].[name] = N'Status');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Recruitments] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Recruitments] ALTER COLUMN [Status] int NOT NULL;
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Recruitments]') AND [c].[name] = N'CreatedAt');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Recruitments] DROP CONSTRAINT [' + @var13 + '];');
GO

ALTER TABLE [Recruitments] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payrolls]') AND [c].[name] = N'Status');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Payrolls] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [Payrolls] ALTER COLUMN [Status] int NOT NULL;
GO

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payrolls]') AND [c].[name] = N'CreatedAt');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Payrolls] DROP CONSTRAINT [' + @var15 + '];');
GO

ALTER TABLE [Payrolls] ADD [AllowanceAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Payrolls] ADD [BaseAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Payrolls] ADD [BonusAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Payrolls] ADD [EmployeeContractId] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Payrolls] ADD [HourlyRate] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Payrolls] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PayrollDetails]') AND [c].[name] = N'DetailType');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [PayrollDetails] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [PayrollDetails] ALTER COLUMN [DetailType] int NOT NULL;
GO

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PayrollDetails]') AND [c].[name] = N'CreatedAt');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [PayrollDetails] DROP CONSTRAINT [' + @var17 + '];');
GO

ALTER TABLE [PayrollDetails] ADD [AttendanceId] int NULL;
GO

ALTER TABLE [PayrollDetails] ADD [Note] nvarchar(250) NULL;
GO

ALTER TABLE [PayrollDetails] ADD [ScheduleId] int NULL;
GO

ALTER TABLE [PayrollDetails] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[KPIs]') AND [c].[name] = N'CreatedAt');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [KPIs] DROP CONSTRAINT [' + @var18 + '];');
GO

ALTER TABLE [KPIs] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeTrainings]') AND [c].[name] = N'Status');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeTrainings] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [EmployeeTrainings] ALTER COLUMN [Status] int NOT NULL;
GO

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeTrainings]') AND [c].[name] = N'AssignedDate');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeTrainings] DROP CONSTRAINT [' + @var20 + '];');
GO

ALTER TABLE [EmployeeTrainings] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [EmployeeTrainings] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'IsActive');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [Employees] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var22 sysname;
SELECT @var22 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'Gender');
IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var22 + '];');
ALTER TABLE [Employees] ALTER COLUMN [Gender] int NOT NULL;
GO

DECLARE @var23 sysname;
SELECT @var23 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'CreatedAt');
IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var23 + '];');
GO

ALTER TABLE [Employees] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var24 sysname;
SELECT @var24 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeContracts]') AND [c].[name] = N'IsActive');
IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeContracts] DROP CONSTRAINT [' + @var24 + '];');
ALTER TABLE [EmployeeContracts] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var25 sysname;
SELECT @var25 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeContracts]') AND [c].[name] = N'CreatedAt');
IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeContracts] DROP CONSTRAINT [' + @var25 + '];');
GO

DECLARE @var26 sysname;
SELECT @var26 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmployeeContracts]') AND [c].[name] = N'ContractType');
IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [EmployeeContracts] DROP CONSTRAINT [' + @var26 + '];');
ALTER TABLE [EmployeeContracts] ALTER COLUMN [ContractType] int NOT NULL;
GO

ALTER TABLE [EmployeeContracts] ADD [EarlyLeavePenaltyPerMinute] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [EmployeeContracts] ADD [HourlyRate] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [EmployeeContracts] ADD [LatePenaltyPerMinute] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [EmployeeContracts] ADD [OvertimeRateMultiplier] decimal(18,2) NOT NULL DEFAULT 1.5;
GO

ALTER TABLE [EmployeeContracts] ADD [StandardDailyHours] decimal(5,2) NOT NULL DEFAULT 8.0;
GO

ALTER TABLE [EmployeeContracts] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var27 sysname;
SELECT @var27 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Candidates]') AND [c].[name] = N'Status');
IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Candidates] DROP CONSTRAINT [' + @var27 + '];');
ALTER TABLE [Candidates] ALTER COLUMN [Status] int NOT NULL;
GO

DECLARE @var28 sysname;
SELECT @var28 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Candidates]') AND [c].[name] = N'AppliedDate');
IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Candidates] DROP CONSTRAINT [' + @var28 + '];');
GO

ALTER TABLE [Candidates] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Candidates] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var29 sysname;
SELECT @var29 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'IsActive');
IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var29 + '];');
ALTER TABLE [Branches] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
GO

DECLARE @var30 sysname;
SELECT @var30 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Branches]') AND [c].[name] = N'CreatedAt');
IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Branches] DROP CONSTRAINT [' + @var30 + '];');
GO

ALTER TABLE [Branches] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [AuditLogs] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

DECLARE @var31 sysname;
SELECT @var31 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'WorkingMinutes');
IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT [' + @var31 + '];');
ALTER TABLE [Attendances] ADD DEFAULT 0 FOR [WorkingMinutes];
GO

DECLARE @var32 sysname;
SELECT @var32 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'LateMinutes');
IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT [' + @var32 + '];');
ALTER TABLE [Attendances] ADD DEFAULT 0 FOR [LateMinutes];
GO

DECLARE @var33 sysname;
SELECT @var33 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'CreatedAt');
IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT [' + @var33 + '];');
GO

ALTER TABLE [Attendances] ADD [EarlyLeaveMinutes] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Attendances] ADD [OvertimeMinutes] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Attendances] ADD [Status] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Attendances] ADD [UpdatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

CREATE INDEX [IX_Schedules_ShiftId_ScheduleDate] ON [Schedules] ([ShiftId], [ScheduleDate]);
GO

CREATE INDEX [IX_Recruitments_Status] ON [Recruitments] ([Status]);
GO

CREATE INDEX [IX_Payrolls_EmployeeContractId] ON [Payrolls] ([EmployeeContractId]);
GO

CREATE INDEX [IX_PayrollDetails_AttendanceId] ON [PayrollDetails] ([AttendanceId]);
GO

CREATE INDEX [IX_PayrollDetails_PayrollId_DetailType] ON [PayrollDetails] ([PayrollId], [DetailType]);
GO

CREATE INDEX [IX_PayrollDetails_ScheduleId] ON [PayrollDetails] ([ScheduleId]);
GO

CREATE INDEX [IX_EmployeeContracts_EmployeeId_IsActive] ON [EmployeeContracts] ([EmployeeId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_Candidates_RecruitmentId_Email] ON [Candidates] ([RecruitmentId], [Email]) WHERE [Email] IS NOT NULL;
GO

CREATE INDEX [IX_AuditLogs_TableName_RecordId] ON [AuditLogs] ([TableName], [RecordId]);
GO

ALTER TABLE [PayrollDetails] ADD CONSTRAINT [FK_PayrollDetails_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [PayrollDetails] ADD CONSTRAINT [FK_PayrollDetails_Schedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [Schedules] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Payrolls] ADD CONSTRAINT [FK_Payrolls_EmployeeContracts_EmployeeContractId] FOREIGN KEY ([EmployeeContractId]) REFERENCES [EmployeeContracts] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260319085553_RefactorHrm', N'8.0.13');
GO

COMMIT;
GO

