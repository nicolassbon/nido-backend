using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    public partial class AddTemaPreferidoToUsuarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tema_preferido",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "system");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tema_preferido",
                table: "usuarios");
        }
    }
}
