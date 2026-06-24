using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260622024500_MergeLegacyCarnesCategory")]
    public partial class MergeLegacyCarnesCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH legacy AS (
                    SELECT id
                    FROM categorias_producto
                    WHERE nombre = 'Carnes'
                    LIMIT 1
                ),
                target AS (
                    SELECT id
                    FROM categorias_producto
                    WHERE nombre = 'Carnes Vacunas'
                    LIMIT 1
                )
                UPDATE productos p
                SET categoria_id = target.id
                FROM legacy, target
                WHERE p.categoria_id = legacy.id;
                """);

            migrationBuilder.Sql("""
                WITH legacy AS (
                    SELECT id
                    FROM categorias_producto
                    WHERE nombre = 'Carnes'
                    LIMIT 1
                ),
                target AS (
                    SELECT id
                    FROM categorias_producto
                    WHERE nombre = 'Carnes Vacunas'
                    LIMIT 1
                )
                UPDATE consumos_producto c
                SET categoria_id = target.id
                FROM legacy, target
                WHERE c.categoria_id = legacy.id;
                """);

            migrationBuilder.Sql("""
                DELETE FROM categorias_producto c
                WHERE c.nombre = 'Carnes'
                  AND EXISTS (
                      SELECT 1
                      FROM categorias_producto target
                      WHERE target.nombre = 'Carnes Vacunas'
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM productos p
                      WHERE p.categoria_id = c.id
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
