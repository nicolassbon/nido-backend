using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevUserAndHousehold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO hogares (id, nombre)
                VALUES ('00000000-0000-0000-0000-000000000001', 'Hogar de Prueba')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO usuarios (id, nombre, email, sexo)
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    'Usuario Dev',
                    'dev@nido.test',
                    'No especificado'
                )
                ON CONFLICT (id) DO UPDATE SET
                    nombre = EXCLUDED.nombre,
                    email = EXCLUDED.email,
                    sexo = EXCLUDED.sexo;

                INSERT INTO miembros_hogar (id, hogar_id, usuario_id, rol)
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    '00000000-0000-0000-0000-000000000001',
                    '00000000-0000-0000-0000-000000000001',
                    'admin'
                )
                ON CONFLICT (id) DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM miembros_hogar
                WHERE id = '00000000-0000-0000-0000-000000000001';
            """);
        }
    }
}
