using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCatalogoElectrodomesticosTiposImagenes : Migration
    {
        /// <inheritdoc />
       protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("""
        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/licuadora.png'
        WHERE id = '11111111-1111-1111-1111-111111111111';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/microondas.png'
        WHERE id = '22222222-2222-2222-2222-222222222222';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/horno.png'
        WHERE id = '33333333-3333-3333-3333-333333333333';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/mixer.png'
        WHERE id = '44444444-4444-4444-4444-444444444444';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/procesadora.png'
        WHERE id = '55555555-5555-5555-5555-555555555555';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/freidora_aire.png'
        WHERE id = '66666666-6666-6666-6666-666666666666';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/cafetera.png'
        WHERE id = '77777777-7777-7777-7777-777777777777';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/tostadora.png'
        WHERE id = '88888888-8888-8888-8888-888888888888';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/olla_presion.png'
        WHERE id = '99999999-9999-9999-9999-999999999999';

        UPDATE electrodomesticos_catalogo
        SET tipo = 'Cocina',
            imagen_url = '/images/parilla_electrica.png'
        WHERE id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    """);
}

        /// <inheritdoc />
       protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("""
        UPDATE electrodomesticos_catalogo
        SET tipo = CASE CAST(id AS TEXT)
            WHEN '11111111-1111-1111-1111-111111111111' THEN 'licuadora'
            WHEN '22222222-2222-2222-2222-222222222222' THEN 'microondas'
            WHEN '33333333-3333-3333-3333-333333333333' THEN 'horno_cocina'
            WHEN '44444444-4444-4444-4444-444444444444' THEN 'mixer'
            WHEN '55555555-5555-5555-5555-555555555555' THEN 'procesadora'
            WHEN '66666666-6666-6666-6666-666666666666' THEN 'freidora_aire'
            WHEN '77777777-7777-7777-7777-777777777777' THEN 'cafetera'
            WHEN '88888888-8888-8888-8888-888888888888' THEN 'tostadora'
            WHEN '99999999-9999-9999-9999-999999999999' THEN 'olla_presion'
            WHEN 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' THEN 'parrilla_electrica'
            ELSE tipo
        END,
        imagen_url = NULL
        WHERE CAST(id AS TEXT) IN (
            '11111111-1111-1111-1111-111111111111',
            '22222222-2222-2222-2222-222222222222',
            '33333333-3333-3333-3333-333333333333',
            '44444444-4444-4444-4444-444444444444',
            '55555555-5555-5555-5555-555555555555',
            '66666666-6666-6666-6666-666666666666',
            '77777777-7777-7777-7777-777777777777',
            '88888888-8888-8888-8888-888888888888',
            '99999999-9999-9999-9999-999999999999',
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
        );
    """);
}
    }
}
