using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPnMedication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PnMedications",
                columns: table => new
                {
                    PnMedicationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CitizenId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftType = table.Column<int>(type: "INTEGER", nullable: false),
                    MedicineName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Dose = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    GivenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GivenByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PnMedications", x => x.PnMedicationId);
                    table.ForeignKey(
                        name: "FK_PnMedications_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "CitizenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PnMedications_CitizenId_GivenAtUtc",
                table: "PnMedications",
                columns: new[] { "CitizenId", "GivenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PnMedications_DepartmentId_GivenAtUtc",
                table: "PnMedications",
                columns: new[] { "DepartmentId", "GivenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PnMedications");
        }
    }
}
