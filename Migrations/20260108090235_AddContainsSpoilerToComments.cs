using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddContainsSpoilerToComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0efc2b1e-fb4b-4062-8685-007a9bf327bc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e9605876-a91a-47a6-ab86-ccde79217e4f");

            migrationBuilder.AddColumn<bool>(
                name: "ContainsSpoiler",
                table: "Comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "b2189c18-08aa-460c-8180-05ec57531cb3", null, "User", "USER" },
                    { "e5991763-0c40-4933-922a-a15bf5556ab4", null, "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b2189c18-08aa-460c-8180-05ec57531cb3");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e5991763-0c40-4933-922a-a15bf5556ab4");

            migrationBuilder.DropColumn(
                name: "ContainsSpoiler",
                table: "Comments");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0efc2b1e-fb4b-4062-8685-007a9bf327bc", null, "Admin", "ADMIN" },
                    { "e9605876-a91a-47a6-ab86-ccde79217e4f", null, "User", "USER" }
                });
        }
    }
}
