using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenStaffAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CitizenStaffAssignments",
                columns: table => new
                {
                    CitizenStaffAssignmentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CitizenId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AssignedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UnassignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnassignedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenStaffAssignments", x => x.CitizenStaffAssignmentId);
                    table.ForeignKey(
                        name: "FK_CitizenStaffAssignments_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "CitizenId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CitizenStaffAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenStaffAssignments_CitizenId_IsActive_AssignedAtUtc",
                table: "CitizenStaffAssignments",
                columns: new[] { "CitizenId", "IsActive", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenStaffAssignments_DepartmentId_IsActive_AssignedAtUtc",
                table: "CitizenStaffAssignments",
                columns: new[] { "DepartmentId", "IsActive", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenStaffAssignments_EmployeeId_IsActive",
                table: "CitizenStaffAssignments",
                columns: new[] { "EmployeeId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitizenStaffAssignments");
        }
    }
}
