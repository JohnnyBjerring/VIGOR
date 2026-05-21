using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedMedicationPlanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FixedMedications",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDescription",
                table: "FixedMedications",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "Dagligt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FixedMedications");

            migrationBuilder.DropColumn(
                name: "ScheduleDescription",
                table: "FixedMedications");
        }
    }
}
