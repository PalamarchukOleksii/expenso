using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpensoServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "Operations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Operations");
        }
    }
}
