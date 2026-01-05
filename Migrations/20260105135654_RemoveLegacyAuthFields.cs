using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "ActivationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0efc2b1e-fb4b-4062-8685-007a9bf327bc", null, "Admin", "ADMIN" },
                    { "e9605876-a91a-47a6-ab86-ccde79217e4f", null, "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0efc2b1e-fb4b-4062-8685-007a9bf327bc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e9605876-a91a-47a6-ab86-ccde79217e4f");

            migrationBuilder.AddColumn<string>(
                name: "ActivationCode",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "07aba40c-eaca-49ab-868b-294dba3a46b5", null, "Admin", "ADMIN" },
                    { "b6d0dd45-ddc8-46e1-bb82-51d29cec1fe8", null, "User", "USER" }
                });
        }
    }
}
