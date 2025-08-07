using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpensoServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOperationModelForTransferOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConvertedAmount",
                table: "Operations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConvertedCurrency",
                table: "Operations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Operations",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedAmount",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ConvertedCurrency",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Operations");
        }
    }
}
