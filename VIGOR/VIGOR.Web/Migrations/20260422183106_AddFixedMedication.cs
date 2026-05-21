using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedMedication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedMedications",
                columns: table => new
                {
                    FixedMedicationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CitizenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PlannedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsGiven = table.Column<bool>(type: "INTEGER", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GivenByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedMedications", x => x.FixedMedicationId);
                    table.ForeignKey(
                        name: "FK_FixedMedications_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "CitizenId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedMedications_CitizenId_PlannedAt",
                table: "FixedMedications",
                columns: new[] { "CitizenId", "PlannedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedMedications");
        }
    }
}
