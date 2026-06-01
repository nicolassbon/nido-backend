using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductosCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                            
            migrationBuilder.Sql("""
    INSERT INTO categorias_producto (id, nombre, ttl_dias)
    VALUES
    ('33333333-3333-3333-3333-333333333333', 'General', 30),
    ('44444444-4444-4444-4444-444444444444', 'Lácteos', 7),
    ('55555555-5555-5555-5555-555555555555', 'Bebidas', 30),
    ('66666666-6666-6666-6666-666666666666', 'Congelados', 90),
    ('77777777-7777-7777-7777-777777777777', 'Despensa', 180)
    ON CONFLICT (id) DO UPDATE SET
        nombre = EXCLUDED.nombre,
        ttl_dias = EXCLUDED.ttl_dias;
""");


            migrationBuilder.Sql("""
        INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id)
        VALUES
        ('10000000-0000-0000-0000-000000000001', 'Leche', NULL, '/productos/leche.png', '44444444-4444-4444-4444-444444444444'),
        ('10000000-0000-0000-0000-000000000002', 'Yogur', NULL, '/productos/yogur.png', '44444444-4444-4444-4444-444444444444'),
        ('10000000-0000-0000-0000-000000000003', 'Queso', NULL, '/productos/queso.png', '44444444-4444-4444-4444-444444444444'),
        ('10000000-0000-0000-0000-000000000004', 'Agua', NULL, '/productos/agua.png', '55555555-5555-5555-5555-555555555555'),
        ('10000000-0000-0000-0000-000000000005', 'Arroz', NULL, '/productos/arroz.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000006', 'Fideos', NULL, '/productos/fideos.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000007', 'Aceite de oliva', NULL, '/productos/aceite-oliva.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000008', 'Sal', NULL, '/productos/sal.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000009', 'Muslo de pollo', NULL, '/productos/muslo-pollo.png', '33333333-3333-3333-3333-333333333333'),
        ('10000000-0000-0000-0000-000000000010', 'Pimentón dulce', NULL, '/productos/pimenton-dulce.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000011', 'Cebolla en polvo', NULL, '/productos/cebolla-polvo.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000012', 'Orégano seco', NULL, '/productos/oregano-seco.png', '77777777-7777-7777-7777-777777777777'),
        ('10000000-0000-0000-0000-000000000013', 'Ajo en polvo', NULL, '/productos/ajo-polvo.png', '77777777-7777-7777-7777-777777777777')
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
            '10000000-0000-0000-0000-000000000001',
            '10000000-0000-0000-0000-000000000002',
            '10000000-0000-0000-0000-000000000003',
            '10000000-0000-0000-0000-000000000004',
            '10000000-0000-0000-0000-000000000005',
            '10000000-0000-0000-0000-000000000006',
            '10000000-0000-0000-0000-000000000007',
            '10000000-0000-0000-0000-000000000008',
            '10000000-0000-0000-0000-000000000009',
            '10000000-0000-0000-0000-000000000010',
            '10000000-0000-0000-0000-000000000011',
            '10000000-0000-0000-0000-000000000012',
            '10000000-0000-0000-0000-000000000013'
        );
    """);

            migrationBuilder.Sql("""
        DELETE FROM categorias_producto
        WHERE id IN (
            '33333333-3333-3333-3333-333333333333',
            '44444444-4444-4444-4444-444444444444',
            '55555555-5555-5555-5555-555555555555',
            '66666666-6666-6666-6666-666666666666',
            '77777777-7777-7777-7777-777777777777'
        );
    """);


        }
    }
}
