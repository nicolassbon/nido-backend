using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260619210000_DeduplicateCategoriasProducto")]
    public partial class DeduplicateCategoriasProducto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH normalized AS (
                    SELECT
                        id,
                        first_value(id) OVER (
                            PARTITION BY lower(regexp_replace(btrim(translate(nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')), '[[:space:]]+', ' ', 'g'))
                            ORDER BY (nombre ~ '[ÁÉÍÓÚáéíóú]') DESC, nombre, id::text
                        ) AS keep_id
                    FROM categorias_producto
                )
                UPDATE productos p
                SET categoria_id = n.keep_id
                FROM normalized n
                WHERE p.categoria_id = n.id
                  AND n.id <> n.keep_id;
                """);

            migrationBuilder.Sql("""
                WITH normalized AS (
                    SELECT
                        id,
                        first_value(id) OVER (
                            PARTITION BY lower(regexp_replace(btrim(translate(nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')), '[[:space:]]+', ' ', 'g'))
                            ORDER BY (nombre ~ '[ÁÉÍÓÚáéíóú]') DESC, nombre, id::text
                        ) AS keep_id
                    FROM categorias_producto
                )
                UPDATE consumos_producto c
                SET categoria_id = n.keep_id
                FROM normalized n
                WHERE c.categoria_id = n.id
                  AND n.id <> n.keep_id;
                """);

            migrationBuilder.Sql("""
                WITH normalized AS (
                    SELECT
                        id,
                        first_value(id) OVER (
                            PARTITION BY lower(regexp_replace(btrim(translate(nombre,
                                'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                                'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')), '[[:space:]]+', ' ', 'g'))
                            ORDER BY (nombre ~ '[ÁÉÍÓÚáéíóú]') DESC, nombre, id::text
                        ) AS keep_id
                    FROM categorias_producto
                )
                DELETE FROM categorias_producto c
                USING normalized n
                WHERE c.id = n.id
                  AND n.id <> n.keep_id;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX categorias_producto_nombre_normalizado_key
                ON categorias_producto (
                    lower(regexp_replace(btrim(translate(nombre,
                        'ÁÀÂÄÃÅáàâäãåÉÈÊËéèêëÍÌÎÏíìîïÓÒÔÖÕóòôöõÚÙÛÜúùûüÑñ',
                        'AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNn')), '[[:space:]]+', ' ', 'g'))
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS categorias_producto_nombre_normalizado_key;");
        }
    }
}
