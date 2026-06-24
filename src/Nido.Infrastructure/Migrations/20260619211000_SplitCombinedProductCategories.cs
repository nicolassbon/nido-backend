using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260619211000_SplitCombinedProductCategories")]
    public partial class SplitCombinedProductCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO categorias_producto (id, nombre, ttl_dias)
                SELECT 'a2000000-0000-0000-0000-000000000001', 'Arroces', 365
                WHERE NOT EXISTS (
                    SELECT 1 FROM categorias_producto
                    WHERE lower(translate(btrim(nombre),
                        'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                        'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'arroces'
                );

                INSERT INTO categorias_producto (id, nombre, ttl_dias)
                SELECT 'a2000000-0000-0000-0000-000000000002', 'Pastas', 365
                WHERE NOT EXISTS (
                    SELECT 1 FROM categorias_producto
                    WHERE lower(translate(btrim(nombre),
                        'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                        'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'pastas'
                );
                """);

            migrationBuilder.Sql("""
                WITH targets AS (
                    SELECT
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'arroces' LIMIT 1) AS arroces_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'pastas' LIMIT 1) AS pastas_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'aceites' LIMIT 1) AS aceites_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'condimentos' LIMIT 1) AS condimentos_id
                ),
                source_categories AS (
                    SELECT
                        c.id,
                        lower(translate(btrim(c.nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) AS normalized_name
                    FROM categorias_producto c
                    WHERE lower(translate(btrim(c.nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn'))
                        IN ('arroces y pastas', 'aceites y condimentos')
                )
                UPDATE productos p
                SET categoria_id = CASE
                    WHEN sc.normalized_name = 'arroces y pastas'
                        THEN CASE
                            WHEN lower(translate(p.nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) LIKE '%arroz%'
                                THEN t.arroces_id
                            ELSE t.pastas_id
                        END
                    WHEN sc.normalized_name = 'aceites y condimentos'
                        THEN CASE
                            WHEN lower(translate(p.nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) LIKE '%aceite%'
                                THEN t.aceites_id
                            ELSE t.condimentos_id
                        END
                    ELSE p.categoria_id
                END
                FROM source_categories sc
                CROSS JOIN targets t
                WHERE p.categoria_id = sc.id;
                """);

            migrationBuilder.Sql("""
                WITH targets AS (
                    SELECT
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'arroces' LIMIT 1) AS arroces_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'pastas' LIMIT 1) AS pastas_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'aceites' LIMIT 1) AS aceites_id,
                        (SELECT id FROM categorias_producto WHERE lower(translate(btrim(nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) = 'condimentos' LIMIT 1) AS condimentos_id
                ),
                source_categories AS (
                    SELECT
                        c.id,
                        lower(translate(btrim(c.nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) AS normalized_name
                    FROM categorias_producto c
                    WHERE lower(translate(btrim(c.nombre),
                            'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                            'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn'))
                        IN ('arroces y pastas', 'aceites y condimentos')
                )
                UPDATE consumos_producto c
                SET categoria_id = CASE
                    WHEN sc.normalized_name = 'arroces y pastas'
                        THEN CASE
                            WHEN lower(translate(c.producto_nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) LIKE '%arroz%'
                                THEN t.arroces_id
                            ELSE t.pastas_id
                        END
                    WHEN sc.normalized_name = 'aceites y condimentos'
                        THEN CASE
                            WHEN lower(translate(c.producto_nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')) LIKE '%aceite%'
                                THEN t.aceites_id
                            ELSE t.condimentos_id
                        END
                    ELSE c.categoria_id
                END
                FROM source_categories sc
                CROSS JOIN targets t
                WHERE c.categoria_id = sc.id;
                """);

            migrationBuilder.Sql("""
                DELETE FROM categorias_producto
                WHERE lower(translate(btrim(nombre),
                    'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                    'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn'))
                IN ('arroces y pastas', 'aceites y condimentos');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM categorias_producto
                WHERE id IN (
                    'a2000000-0000-0000-0000-000000000001',
                    'a2000000-0000-0000-0000-000000000002'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM productos p WHERE p.categoria_id = categorias_producto.id
                )
                AND NOT EXISTS (
                    SELECT 1 FROM consumos_producto c WHERE c.categoria_id = categorias_producto.id
                );
                """);
        }
    }
}
