using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260620093000_SeedRecipeShoppingPurchaseStandards")]
    public partial class SeedRecipeShoppingPurchaseStandards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
                SELECT '10000000-0000-0000-0000-000000000022', 'Ajo', NULL, NULL, '77777777-7777-7777-7777-777777777777'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM productos
                    WHERE lower(btrim(nombre)) = 'ajo'
                );

                INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
                SELECT '10000000-0000-0000-0000-000000000023', 'Azucar', NULL, NULL, '77777777-7777-7777-7777-777777777777'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM productos
                    WHERE lower(btrim(nombre)) = 'azucar'
                );

                INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
                SELECT '10000000-0000-0000-0000-000000000024', 'Chauchas', NULL, NULL, '33333333-3333-3333-3333-333333333333'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM productos
                    WHERE lower(btrim(nombre)) = 'chauchas'
                );

                INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
                SELECT '10000000-0000-0000-0000-000000000025', 'Cebolla de verdeo', NULL, NULL, '33333333-3333-3333-3333-333333333333'
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM productos
                    WHERE lower(btrim(nombre)) = 'cebolla de verdeo'
                );

                UPDATE productos
                SET cantidad_compra_estandar = 1.00,
                    unidad_compra_estandar = 'lt'
                WHERE lower(btrim(nombre)) IN ('aceite de oliva', 'leche', 'agua');

                UPDATE productos
                SET cantidad_compra_estandar = 1.00,
                    unidad_compra_estandar = 'kg'
                WHERE lower(btrim(nombre)) IN ('arroz', 'harina', 'azucar', 'azúcar rubia', 'azucar rubia', 'sal', 'chauchas');

                UPDATE productos
                SET cantidad_compra_estandar = 1.00,
                    unidad_compra_estandar = 'unidad'
                WHERE lower(btrim(nombre)) IN ('ajo', 'ajo en polvo', 'manteca', 'fideos', 'arvejas', 'cebolla', 'cebolla de verdeo', 'queso rallado');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE productos
                SET cantidad_compra_estandar = NULL,
                    unidad_compra_estandar = NULL
                WHERE id IN (
                    '10000000-0000-0000-0000-000000000004',
                    '10000000-0000-0000-0000-000000000005',
                    '10000000-0000-0000-0000-000000000006',
                    '10000000-0000-0000-0000-000000000007',
                    '10000000-0000-0000-0000-000000000008',
                    '10000000-0000-0000-0000-000000000013',
                    '10000000-0000-0000-0000-000000000014',
                    '10000000-0000-0000-0000-000000000016',
                    '10000000-0000-0000-0000-000000000019',
                    '10000000-0000-0000-0000-000000000020',
                    '10000000-0000-0000-0000-000000000022',
                    '10000000-0000-0000-0000-000000000023',
                    '10000000-0000-0000-0000-000000000024',
                    '10000000-0000-0000-0000-000000000025'
                )
                OR lower(btrim(nombre)) IN ('azúcar rubia', 'azucar rubia', 'queso rallado');

                DELETE FROM productos
                WHERE id IN (
                    '10000000-0000-0000-0000-000000000022',
                    '10000000-0000-0000-0000-000000000023',
                    '10000000-0000-0000-0000-000000000024',
                    '10000000-0000-0000-0000-000000000025'
                );
                """);
        }
    }
}
