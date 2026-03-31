using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIGOR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Citizens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Citizens");
        }
    }
}
