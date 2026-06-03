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
        ('10000000-0000-0000-0000-000000000017', 'Sal', NULL, '/productos/sal.png', '77777777-7777-7777-7777-777777777777'),
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
}

        /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
{
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
