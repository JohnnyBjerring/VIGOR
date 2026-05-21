using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    AuditEventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    UserDisplayNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CitizenId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftType = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.AuditEventId);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "CitizenId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action_CreatedAtUtc",
                table: "AuditEvents",
                columns: new[] { "Action", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CitizenId_CreatedAtUtc",
                table: "AuditEvents",
                columns: new[] { "CitizenId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_DepartmentId_CreatedAtUtc",
                table: "AuditEvents",
                columns: new[] { "DepartmentId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
