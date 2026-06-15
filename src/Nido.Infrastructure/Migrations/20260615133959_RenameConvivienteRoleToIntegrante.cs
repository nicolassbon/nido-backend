using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameConvivienteRoleToIntegrante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE miembros_hogar
                SET rol = 'integrante'
                WHERE rol = 'conviviente';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE miembros_hogar
                SET rol = 'conviviente'
                WHERE rol = 'integrante';
                """);
        }
    }
}
