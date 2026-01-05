using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbIdToFilms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "14a2e957-d88c-427d-822b-6aae35aa79b4");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "6d77a4dd-d6d2-4d70-8d47-9e8abe552fb1");

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Films",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "07aba40c-eaca-49ab-868b-294dba3a46b5", null, "Admin", "ADMIN" },
                    { "b6d0dd45-ddc8-46e1-bb82-51d29cec1fe8", null, "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "07aba40c-eaca-49ab-868b-294dba3a46b5");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b6d0dd45-ddc8-46e1-bb82-51d29cec1fe8");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Films");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "14a2e957-d88c-427d-822b-6aae35aa79b4", null, "Admin", "ADMIN" },
                    { "6d77a4dd-d6d2-4d70-8d47-9e8abe552fb1", null, "User", "USER" }
                });
        }
    }
}
