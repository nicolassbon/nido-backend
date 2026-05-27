using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedHogarElectrodomesticos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO hogares (id, nombre)
                VALUES (
                    '83e0bb2b-8585-469c-86d7-802cddb2434a',
                    'Hogar de prueba'
                )
                ON CONFLICT (id) DO UPDATE
                SET nombre = EXCLUDED.nombre;
            """);

            migrationBuilder.Sql("""
                INSERT INTO electrodomesticos (id, hogar_id, nombre, tipo, estado)
                VALUES
                (
                    '9c36a44c-e6cd-4992-aac0-5311c27e6f6e',
                    '83e0bb2b-8585-469c-86d7-802cddb2434a',
                    'Heladera',
                    'Cocina',
                    'Activo'
                ),
                (
                    'd1d1c8a1-3b09-4af0-9d7f-8b9a3c1d2e4f',
                    '83e0bb2b-8585-469c-86d7-802cddb2434a',
                    'Lavarropas',
                    'Lavadero',
                    'Necesita mantenimiento'
                )
                ON CONFLICT (id) DO UPDATE
                SET
                    hogar_id = EXCLUDED.hogar_id,
                    nombre = EXCLUDED.nombre,
                    tipo = EXCLUDED.tipo,
                    estado = EXCLUDED.estado;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM electrodomesticos
                WHERE id IN (
                    '9c36a44c-e6cd-4992-aac0-5311c27e6f6e',
                    'd1d1c8a1-3b09-4af0-9d7f-8b9a3c1d2e4f'
                );
            """);

            migrationBuilder.Sql("""
                DELETE FROM hogares
                WHERE id = '83e0bb2b-8585-469c-86d7-802cddb2434a';
            """);
        }
    }
}