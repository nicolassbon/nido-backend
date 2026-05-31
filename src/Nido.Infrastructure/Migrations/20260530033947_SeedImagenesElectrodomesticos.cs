using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedImagenesElectrodomesticos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        { migrationBuilder.Sql("""
        UPDATE electrodomesticos
        SET
            marca = 'Samsung',
            imagen_url = '/images/heladera.png'
        WHERE nombre = 'Heladera';
    """);

    migrationBuilder.Sql("""
        UPDATE electrodomesticos
        SET
            marca = 'Drean',
            imagen_url = '/images/lavarropas.png'
        WHERE nombre = 'Lavarropas';
    """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
   migrationBuilder.Sql("""
        UPDATE electrodomesticos
        SET
            marca = NULL,
            imagen_url = NULL
        WHERE nombre IN ('Heladera', 'Lavarropas');
    """);
        }
    }
}
