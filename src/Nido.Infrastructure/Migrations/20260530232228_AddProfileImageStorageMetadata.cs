using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageStorageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "foto_content_type",
                table: "usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "foto_height",
                table: "usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "foto_size_bytes",
                table: "usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "foto_storage_key",
                table: "usuarios",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "foto_width",
                table: "usuarios",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "foto_content_type",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "foto_height",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "foto_size_bytes",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "foto_storage_key",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "foto_width",
                table: "usuarios");
        }
    }
}
