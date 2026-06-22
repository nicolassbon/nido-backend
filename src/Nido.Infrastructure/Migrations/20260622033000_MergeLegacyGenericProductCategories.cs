using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260622033000_MergeLegacyGenericProductCategories")]
    public partial class MergeLegacyGenericProductCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH targets AS (
                    SELECT
                        (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1) AS frutas_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1) AS carnes_porcinas_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1) AS pescados_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1) AS verduras_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1) AS legumbres_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1) AS panificados_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1) AS pastas_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Arroz' LIMIT 1) AS arroz_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Cereales' LIMIT 1) AS cereales_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1) AS harinas_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1) AS azucar_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Repostería' LIMIT 1) AS reposteria_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1) AS aceites_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1) AS condimentos_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1) AS salsas_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Huevos' LIMIT 1) AS huevos_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1) AS higiene_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1) AS otros_id
                )
                UPDATE productos p
                SET categoria_id = COALESCE(
                    CASE
                        WHEN p.categoria_id = (SELECT id FROM categorias_producto WHERE nombre = 'Baño' LIMIT 1) THEN t.higiene_id
                        WHEN lower(p.nombre) LIKE '%salchicha%' OR lower(p.nombre) LIKE '%chorizo%' OR lower(p.nombre) LIKE '%panceta%' OR lower(p.nombre) LIKE '%jamon%' OR lower(p.nombre) LIKE '%jamón%' THEN t.carnes_porcinas_id
                        WHEN lower(p.nombre) LIKE '%salmon%' OR lower(p.nombre) LIKE '%salmón%' OR lower(p.nombre) LIKE '%pescado%' OR lower(p.nombre) LIKE '%atun%' OR lower(p.nombre) LIKE '%atún%' OR lower(p.nombre) LIKE '%merluza%' THEN t.pescados_id
                        WHEN lower(p.nombre) LIKE '%manzana%' OR lower(p.nombre) LIKE '%banana%' OR lower(p.nombre) LIKE '%naranja%' OR lower(p.nombre) LIKE '%limon%' OR lower(p.nombre) LIKE '%frutilla%' OR lower(p.nombre) LIKE '%fruta%' THEN t.frutas_id
                        WHEN lower(p.nombre) LIKE '%ajo en polvo%' OR lower(p.nombre) LIKE '%cebolla en polvo%' OR lower(p.nombre) LIKE '%pimenton%' OR lower(p.nombre) LIKE '%pimentón%' OR lower(p.nombre) LIKE '%aji molido%' OR lower(p.nombre) LIKE '%ají molido%' THEN t.condimentos_id
                        WHEN lower(p.nombre) LIKE '%tomate%' OR lower(p.nombre) LIKE '%zanahoria%' OR lower(p.nombre) LIKE '%cebolla%' OR lower(p.nombre) LIKE '%lechuga%' OR lower(p.nombre) LIKE '%papa%' OR lower(p.nombre) LIKE '%batata%' OR lower(p.nombre) LIKE '%morron%' OR lower(p.nombre) LIKE '%ajo%' OR lower(p.nombre) LIKE '%verdura%' OR lower(p.nombre) LIKE '%zapallo%' THEN t.verduras_id
                        WHEN lower(p.nombre) LIKE '%lenteja%' OR lower(p.nombre) LIKE '%garbanzo%' OR lower(p.nombre) LIKE '%poroto%' OR lower(p.nombre) LIKE '%arveja%' OR lower(p.nombre) LIKE '%legumbre%' THEN t.legumbres_id
                        WHEN lower(p.nombre) LIKE '%pan%' OR lower(p.nombre) LIKE '%medialuna%' OR lower(p.nombre) LIKE '%tostada%' THEN t.panificados_id
                        WHEN lower(p.nombre) LIKE '%fideo%' OR lower(p.nombre) LIKE '%pasta%' OR lower(p.nombre) LIKE '%spaghetti%' OR lower(p.nombre) LIKE '%raviol%' THEN t.pastas_id
                        WHEN lower(p.nombre) LIKE '%arroz%' THEN t.arroz_id
                        WHEN lower(p.nombre) LIKE '%cereal%' OR lower(p.nombre) LIKE '%avena%' OR lower(p.nombre) LIKE '%granola%' THEN t.cereales_id
                        WHEN lower(p.nombre) LIKE '%harina%' OR lower(p.nombre) LIKE '%maicena%' THEN t.harinas_id
                        WHEN lower(p.nombre) LIKE '%azucar%' OR lower(p.nombre) LIKE '%azúcar%' OR lower(p.nombre) LIKE '%edulcorante%' OR lower(p.nombre) LIKE '%miel%' THEN t.azucar_id
                        WHEN lower(p.nombre) LIKE '%chocolate%' OR lower(p.nombre) LIKE '%levadura%' OR lower(p.nombre) LIKE '%esencia%' OR lower(p.nombre) LIKE '%reposteria%' OR lower(p.nombre) LIKE '%repostería%' THEN t.reposteria_id
                        WHEN lower(p.nombre) LIKE '%aceite%' THEN t.aceites_id
                        WHEN lower(p.nombre) LIKE '%sal%' OR lower(p.nombre) LIKE '%pimienta%' OR lower(p.nombre) LIKE '%oregano%' OR lower(p.nombre) LIKE '%orégano%' OR lower(p.nombre) LIKE '%condimento%' OR lower(p.nombre) LIKE '%caldo%' THEN t.condimentos_id
                        WHEN lower(p.nombre) LIKE '%salsa%' OR lower(p.nombre) LIKE '%aderezo%' OR lower(p.nombre) LIKE '%mayonesa%' OR lower(p.nombre) LIKE '%ketchup%' OR lower(p.nombre) LIKE '%mostaza%' OR lower(p.nombre) LIKE '%vinagre%' THEN t.salsas_id
                        WHEN lower(p.nombre) LIKE '%huevo%' THEN t.huevos_id
                        ELSE t.otros_id
                    END,
                    p.categoria_id
                )
                FROM targets t
                WHERE p.categoria_id IS NULL
                   OR p.categoria_id IN (
                       SELECT id
                       FROM categorias_producto
                       WHERE nombre IN ('Almacén', 'Baño')
                   );
                """);

            migrationBuilder.Sql("""
                WITH targets AS (
                    SELECT
                        (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1) AS higiene_id,
                        (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1) AS otros_id
                ),
                legacy AS (
                    SELECT id, nombre
                    FROM categorias_producto
                    WHERE nombre IN ('Almacén', 'Baño')
                )
                UPDATE consumos_producto c
                SET categoria_id = CASE
                    WHEN l.nombre = 'Baño' THEN t.higiene_id
                    ELSE t.otros_id
                END
                FROM legacy l, targets t
                WHERE c.categoria_id = l.id;
                """);

            migrationBuilder.Sql("""
                DELETE FROM categorias_producto c
                WHERE c.nombre IN ('Almacén', 'Baño')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM productos p
                      WHERE p.categoria_id = c.id
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM consumos_producto cp
                      WHERE cp.categoria_id = c.id
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
