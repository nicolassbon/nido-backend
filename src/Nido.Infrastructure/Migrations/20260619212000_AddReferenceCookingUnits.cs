using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260619212000_AddReferenceCookingUnits")]
    public partial class AddReferenceCookingUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM unidades_medida WHERE codigo = 'g') THEN
                        DELETE FROM unidades_medida WHERE codigo = 'gr';
                    ELSE
                        UPDATE unidades_medida
                        SET codigo = 'g'
                        WHERE codigo = 'gr';
                    END IF;
                END $$;

                UPDATE unidades_medida
                SET nombre = 'Gramo'
                WHERE codigo = 'g';

                UPDATE unidades_medida
                SET nombre = 'Kilogramo'
                WHERE codigo = 'kg';

                UPDATE unidades_medida
                SET nombre = 'Mililitro'
                WHERE codigo = 'ml';

                UPDATE unidades_medida
                SET nombre = 'Litro'
                WHERE codigo = 'lt';

                UPDATE unidades_medida
                SET nombre = 'Cucharadita'
                WHERE codigo = 'cdita';

                UPDATE unidades_medida
                SET nombre = 'Cucharada'
                WHERE codigo = 'cda';

                INSERT INTO unidades_medida (id, codigo, nombre)
                VALUES
                  ('c1000000-0000-0000-0000-000000000008', 'taza', 'Taza'),
                  ('c1000000-0000-0000-0000-000000000009', 'vaso', 'Vaso'),
                  ('c1000000-0000-0000-0000-000000000010', 'pizca', 'Pizca'),
                  ('c1000000-0000-0000-0000-000000000011', '1/2_cdita', '1/2 cucharadita'),
                  ('c1000000-0000-0000-0000-000000000012', '1/2_cda', '1/2 cucharada'),
                  ('c1000000-0000-0000-0000-000000000013', '1/4_taza', '1/4 taza'),
                  ('c1000000-0000-0000-0000-000000000014', '1/2_taza', '1/2 taza'),
                  ('c1000000-0000-0000-0000-000000000015', '3/4_taza', '3/4 taza')
                ON CONFLICT (codigo) DO UPDATE
                SET nombre = EXCLUDED.nombre;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE unidades_medida
                SET codigo = 'gr',
                    nombre = 'Gramos (gr)'
                WHERE codigo = 'g';

                DELETE FROM unidades_medida
                WHERE codigo IN (
                    'taza',
                    'vaso',
                    'pizca',
                    '1/2_cdita',
                    '1/2_cda',
                    '1/4_taza',
                    '1/2_taza',
                    '3/4_taza'
                );
                """);
        }
    }
}
