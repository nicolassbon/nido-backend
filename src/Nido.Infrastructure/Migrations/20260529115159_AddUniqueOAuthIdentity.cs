using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOAuthIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_usuarios_oauth_identity",
                table: "usuarios",
                columns: new[] { "oauth_provider", "oauth_id" },
                unique: true,
                filter: "oauth_provider IS NOT NULL AND oauth_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_usuarios_oauth_identity",
                table: "usuarios");
        }
    }
}
