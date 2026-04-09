using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NguyenDacQuan_2123110483.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedHrmWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserAccountId",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserAccountId",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceAmount",
                table: "Payrolls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Payrolls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Payrolls",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Payrolls",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AttendanceAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestedCheckInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedCheckOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedStatus = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserAccountId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustments_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAdjustments_UserAccounts_ReviewedByUserAccountId",
                        column: x => x.ReviewedByUserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalDays = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserAccountId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_UserAccounts_ReviewedByUserAccountId",
                        column: x => x.ReviewedByUserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollClosePeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollMonth = table.Column<int>(type: "int", nullable: false),
                    PayrollYear = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserAccountId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollClosePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollClosePeriods_UserAccounts_ClosedByUserAccountId",
                        column: x => x.ClosedByUserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestEmployeeId = table.Column<int>(type: "int", nullable: false),
                    TargetEmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestScheduleId = table.Column<int>(type: "int", nullable: false),
                    TargetScheduleId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserAccountId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_RequestEmployeeId",
                        column: x => x.RequestEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_TargetEmployeeId",
                        column: x => x.TargetEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Schedules_RequestScheduleId",
                        column: x => x.RequestScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Schedules_TargetScheduleId",
                        column: x => x.TargetScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_UserAccounts_ReviewedByUserAccountId",
                        column: x => x.ReviewedByUserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ApprovedByUserAccountId",
                table: "Payrolls",
                column: "ApprovedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ClosedByUserAccountId",
                table: "Payrolls",
                column: "ClosedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_PayrollMonth_PayrollYear_IsClosed",
                table: "Payrolls",
                columns: new[] { "PayrollMonth", "PayrollYear", "IsClosed" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustments_AttendanceId_Status",
                table: "AttendanceAdjustments",
                columns: new[] { "AttendanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustments_EmployeeId",
                table: "AttendanceAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustments_ReviewedByUserAccountId",
                table: "AttendanceAdjustments",
                column: "ReviewedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_StartDate_EndDate",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ReviewedByUserAccountId",
                table: "LeaveRequests",
                column: "ReviewedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollClosePeriods_ClosedByUserAccountId",
                table: "PayrollClosePeriods",
                column: "ClosedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollClosePeriods_PayrollMonth_PayrollYear",
                table: "PayrollClosePeriods",
                columns: new[] { "PayrollMonth", "PayrollYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequestEmployeeId",
                table: "ShiftSwapRequests",
                column: "RequestEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequestScheduleId_TargetScheduleId",
                table: "ShiftSwapRequests",
                columns: new[] { "RequestScheduleId", "TargetScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_ReviewedByUserAccountId",
                table: "ShiftSwapRequests",
                column: "ReviewedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetEmployeeId",
                table: "ShiftSwapRequests",
                column: "TargetEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetScheduleId",
                table: "ShiftSwapRequests",
                column: "TargetScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_UserAccounts_ApprovedByUserAccountId",
                table: "Payrolls",
                column: "ApprovedByUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_UserAccounts_ClosedByUserAccountId",
                table: "Payrolls",
                column: "ClosedByUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_UserAccounts_ApprovedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_UserAccounts_ClosedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropTable(
                name: "AttendanceAdjustments");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "PayrollClosePeriods");

            migrationBuilder.DropTable(
                name: "ShiftSwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_ApprovedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_ClosedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_PayrollMonth_PayrollYear_IsClosed",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ClosedByUserAccountId",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "InsuranceAmount",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Payrolls");
        }
    }
}
