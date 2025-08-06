using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpensoServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("01f288f5-1a95-4da1-a4dc-11ba0ce5d312"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("19e457c8-1579-4b1a-8ea2-6240c61e6ccc"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("6811c3a6-2cb0-4dc7-b5ba-31392d2b90f7"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("93fbc4a4-1bf3-4513-a070-42f3e608c765"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("ad31896d-60c0-49de-ad67-e55821deca8b"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("eef29ec0-98fe-4d21-bd6e-9e3624366b62"));

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsDefault", "Name", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("181f61df-3da5-4f89-ab05-8b0718d25aa2"), true, "Salary", 0, null },
                    { new Guid("430b7c2c-bdd5-4bee-8609-e08c8f406a39"), true, "Food", 1, null },
                    { new Guid("bc539863-1619-4d83-a168-2b828f694c3e"), true, "Home", 1, null },
                    { new Guid("ceca4e62-81f6-4aa7-b37c-9f57b0ef4a71"), true, "Investments", 0, null },
                    { new Guid("ddabd24b-40c3-4a3b-aa9b-3de111054a63"), true, "Other", 0, null },
                    { new Guid("eb34eabf-891c-4eee-98d2-c64c4315055d"), true, "Other", 1, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("181f61df-3da5-4f89-ab05-8b0718d25aa2"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("430b7c2c-bdd5-4bee-8609-e08c8f406a39"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("bc539863-1619-4d83-a168-2b828f694c3e"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("ceca4e62-81f6-4aa7-b37c-9f57b0ef4a71"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("ddabd24b-40c3-4a3b-aa9b-3de111054a63"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("eb34eabf-891c-4eee-98d2-c64c4315055d"));

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IsDefault", "Name", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("01f288f5-1a95-4da1-a4dc-11ba0ce5d312"), true, "Other", 1, null },
                    { new Guid("19e457c8-1579-4b1a-8ea2-6240c61e6ccc"), true, "Other", 0, null },
                    { new Guid("6811c3a6-2cb0-4dc7-b5ba-31392d2b90f7"), true, "Food", 1, null },
                    { new Guid("93fbc4a4-1bf3-4513-a070-42f3e608c765"), true, "Investments", 0, null },
                    { new Guid("ad31896d-60c0-49de-ad67-e55821deca8b"), true, "Home", 1, null },
                    { new Guid("eef29ec0-98fe-4d21-bd6e-9e3624366b62"), true, "Salary", 0, null }
                });
        }
    }
}
