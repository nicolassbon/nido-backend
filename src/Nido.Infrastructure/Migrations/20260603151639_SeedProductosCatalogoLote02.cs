using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductosCatalogoLote02 : Migration
    {
        /// <inheritdoc />
       protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("""
        INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
        VALUES
        ('10000000-0000-0000-0000-000000000014', 'Arvejas', NULL, '/productos/arvejas.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000015', 'Pasas de uva', NULL, '/productos/pasas-uva.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000016', 'Harina', NULL, '/productos/harina.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000018', 'Salmón', NULL, '/productos/salmon.png', '33333333-3333-3333-3333-333333333333'),
        ('10000000-0000-0000-0000-000000000019', 'Manteca', NULL, '/productos/manteca.png', '44444444-4444-4444-4444-444444444444'),
        ('10000000-0000-0000-0000-000000000020', 'Cebolla', NULL, '/productos/cebolla.png', '33333333-3333-3333-3333-333333333333'),
        ('10000000-0000-0000-0000-000000000021', 'Salchicha', NULL, '/productos/salchicha.png', '33333333-3333-3333-3333-333333333333')
        ON CONFLICT (id) DO UPDATE SET
            nombre = EXCLUDED.nombre,
            codigo_barras = EXCLUDED.codigo_barras,
            imagen_url = EXCLUDED.imagen_url,
            categoria_id = EXCLUDED.categoria_id;
    """);

    migrationBuilder.Sql("""
        UPDATE ingredientes_receta
        SET producto_id = CASE
            WHEN LOWER(nombre_ingrediente) LIKE '%arvejas%' THEN '10000000-0000-0000-0000-000000000014'
            WHEN LOWER(nombre_ingrediente) LIKE '%pasas de uva%' THEN '10000000-0000-0000-0000-000000000015'
            WHEN LOWER(nombre_ingrediente) LIKE '%harina%' THEN '10000000-0000-0000-0000-000000000016'
            WHEN LOWER(nombre_ingrediente) LIKE '%salmón%' OR LOWER(nombre_ingrediente) LIKE '%salmon%' THEN '10000000-0000-0000-0000-000000000018'
            WHEN LOWER(nombre_ingrediente) LIKE '%manteca%' OR LOWER(nombre_ingrediente) LIKE '%mantequilla%' THEN '10000000-0000-0000-0000-000000000019'
            WHEN LOWER(nombre_ingrediente) LIKE '%cebolla%'
                 AND LOWER(nombre_ingrediente) NOT LIKE '%verdeo%'
                 AND LOWER(nombre_ingrediente) NOT LIKE '%polvo%' THEN '10000000-0000-0000-0000-000000000020'
            WHEN LOWER(nombre_ingrediente) LIKE '%salchicha%' THEN '10000000-0000-0000-0000-000000000021'
            WHEN LOWER(nombre_ingrediente) LIKE '%queso%' THEN '10000000-0000-0000-0000-000000000003'
            WHEN LOWER(nombre_ingrediente) LIKE '%arroz%' THEN '10000000-0000-0000-0000-000000000005'
            WHEN LOWER(nombre_ingrediente) LIKE '%aceite%' THEN '10000000-0000-0000-0000-000000000007'
            WHEN LOWER(nombre_ingrediente) = 'sal'
                 OR LOWER(nombre_ingrediente) LIKE 'sal %'
                 OR LOWER(nombre_ingrediente) LIKE '% sal'
                 OR LOWER(nombre_ingrediente) LIKE '% sal %' THEN '10000000-0000-0000-0000-000000000008'
            WHEN LOWER(nombre_ingrediente) LIKE '%pimentón%' OR LOWER(nombre_ingrediente) LIKE '%pimenton%' THEN '10000000-0000-0000-0000-000000000010'
            WHEN LOWER(nombre_ingrediente) LIKE '%agua%' THEN '10000000-0000-0000-0000-000000000004'
            ELSE producto_id
        END
        WHERE producto_id IS NULL
          AND (
              LOWER(nombre_ingrediente) LIKE '%arvejas%'
              OR LOWER(nombre_ingrediente) LIKE '%pasas de uva%'
              OR LOWER(nombre_ingrediente) LIKE '%harina%'
              OR LOWER(nombre_ingrediente) LIKE '%salmón%'
              OR LOWER(nombre_ingrediente) LIKE '%salmon%'
              OR LOWER(nombre_ingrediente) LIKE '%manteca%'
              OR LOWER(nombre_ingrediente) LIKE '%mantequilla%'
              OR LOWER(nombre_ingrediente) LIKE '%cebolla%'
              OR LOWER(nombre_ingrediente) LIKE '%salchicha%'
              OR LOWER(nombre_ingrediente) LIKE '%queso%'
              OR LOWER(nombre_ingrediente) LIKE '%arroz%'
              OR LOWER(nombre_ingrediente) LIKE '%aceite%'
              OR LOWER(nombre_ingrediente) = 'sal'
              OR LOWER(nombre_ingrediente) LIKE 'sal %'
              OR LOWER(nombre_ingrediente) LIKE '% sal'
              OR LOWER(nombre_ingrediente) LIKE '% sal %'
              OR LOWER(nombre_ingrediente) LIKE '%pimentón%'
              OR LOWER(nombre_ingrediente) LIKE '%pimenton%'
              OR LOWER(nombre_ingrediente) LIKE '%agua%'
          );
    """);
}

        /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("""
        UPDATE ingredientes_receta
        SET producto_id = NULL
        WHERE producto_id IN (
            '10000000-0000-0000-0000-000000000003',
            '10000000-0000-0000-0000-000000000004',
            '10000000-0000-0000-0000-000000000005',
            '10000000-0000-0000-0000-000000000007',
            '10000000-0000-0000-0000-000000000008',
            '10000000-0000-0000-0000-000000000010',
            '10000000-0000-0000-0000-000000000014',
            '10000000-0000-0000-0000-000000000015',
            '10000000-0000-0000-0000-000000000016',
            '10000000-0000-0000-0000-000000000018',
            '10000000-0000-0000-0000-000000000019',
            '10000000-0000-0000-0000-000000000020',
            '10000000-0000-0000-0000-000000000021'
        )
        AND (
            LOWER(nombre_ingrediente) LIKE '%arvejas%'
            OR LOWER(nombre_ingrediente) LIKE '%pasas de uva%'
            OR LOWER(nombre_ingrediente) LIKE '%harina%'
            OR LOWER(nombre_ingrediente) LIKE '%salmón%'
            OR LOWER(nombre_ingrediente) LIKE '%salmon%'
            OR LOWER(nombre_ingrediente) LIKE '%manteca%'
            OR LOWER(nombre_ingrediente) LIKE '%mantequilla%'
            OR LOWER(nombre_ingrediente) LIKE '%cebolla%'
            OR LOWER(nombre_ingrediente) LIKE '%salchicha%'
            OR LOWER(nombre_ingrediente) LIKE '%queso%'
            OR LOWER(nombre_ingrediente) LIKE '%arroz%'
            OR LOWER(nombre_ingrediente) LIKE '%aceite%'
            OR LOWER(nombre_ingrediente) = 'sal'
            OR LOWER(nombre_ingrediente) LIKE 'sal %'
            OR LOWER(nombre_ingrediente) LIKE '% sal'
            OR LOWER(nombre_ingrediente) LIKE '% sal %'
            OR LOWER(nombre_ingrediente) LIKE '%pimentón%'
            OR LOWER(nombre_ingrediente) LIKE '%pimenton%'
            OR LOWER(nombre_ingrediente) LIKE '%agua%'
        );
    """);

    migrationBuilder.Sql("""
        DELETE FROM productos
        WHERE id IN (
            '10000000-0000-0000-0000-000000000014',
            '10000000-0000-0000-0000-000000000015',
            '10000000-0000-0000-0000-000000000016',
            '10000000-0000-0000-0000-000000000017',
            '10000000-0000-0000-0000-000000000018',
            '10000000-0000-0000-0000-000000000019',
            '10000000-0000-0000-0000-000000000020',
            '10000000-0000-0000-0000-000000000021'
        );
    """);
}
    }
}
