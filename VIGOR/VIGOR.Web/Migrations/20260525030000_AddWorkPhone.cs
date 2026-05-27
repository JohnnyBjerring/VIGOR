using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkPhones",
                columns: table => new
                {
                    WorkPhoneId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPhones", x => x.WorkPhoneId);
                });

            migrationBuilder.CreateTable(
                name: "PhoneAssignments",
                columns: table => new
                {
                    PhoneAssignmentId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkPhoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PhoneLabelSnapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PhoneNumberSnapshot = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AssignedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UnassignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnassignedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneAssignments", x => x.PhoneAssignmentId);
                    table.ForeignKey(
                        name: "FK_PhoneAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneAssignments_WorkPhones_WorkPhoneId",
                        column: x => x.WorkPhoneId,
                        principalTable: "WorkPhones",
                        principalColumn: "WorkPhoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPhones_PhoneNumber",
                table: "WorkPhones",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneAssignments_DepartmentId_IsActive_AssignedAtUtc",
                table: "PhoneAssignments",
                columns: new[] { "DepartmentId", "IsActive", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneAssignments_EmployeeId_IsActive",
                table: "PhoneAssignments",
                columns: new[] { "EmployeeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneAssignments_WorkPhoneId_IsActive",
                table: "PhoneAssignments",
                columns: new[] { "WorkPhoneId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneAssignments");

            migrationBuilder.DropTable(
                name: "WorkPhones");
        }
    }
}
