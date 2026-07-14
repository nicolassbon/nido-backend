using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260714150230_SeedBreakfastDemoRecipes")]
    public partial class SeedBreakfastDemoRecipes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM recetas
                        WHERE id IN ('71600000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000002')
                           OR nombre IN ('Tostadas de huevo revuelto con espinaca y queso cremoso', 'Licuado de banana, avena y dulce de leche')
                           OR fuente_id = 'nido-breakfast-demo'
                    ) OR EXISTS (
                        SELECT 1 FROM ingredientes_receta
                        WHERE id BETWEEN '71610000-0000-0000-0000-000000000001' AND '71610000-0000-0000-0000-000000000012'
                    ) OR EXISTS (
                        SELECT 1 FROM pasos_receta
                        WHERE id BETWEEN '71630000-0000-0000-0000-000000000001' AND '71630000-0000-0000-0000-000000000011'
                    ) OR EXISTS (
                        SELECT 1 FROM info_nutricional_receta
                        WHERE id IN ('71620000-0000-0000-0000-000000000001', '71620000-0000-0000-0000-000000000002')
                    ) OR EXISTS (
                        SELECT 1 FROM receta_electrodomestico
                        WHERE id IN ('71640000-0000-0000-0000-000000000001', '71640000-0000-0000-0000-000000000002')
                    ) THEN
                        RAISE EXCEPTION 'Breakfast demo seed collision: deterministic IDs, recipe names, or source marker already exist; resolve the existing data before applying this migration.';
                    END IF;
                END $$;
                """);

            // User-approved image provenance: Pexels photo 34871729 and Unsplash photo 1685967836529-b0e8d6938227.
            migrationBuilder.Sql("""
                INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url)
                VALUES
                    ('71600000-0000-0000-0000-000000000001', 'Tostadas de huevo revuelto con espinaca y queso cremoso', 'Tostadas doradas con huevo revuelto, espinaca fresca salteada y queso cremoso fundido; una opción completa y rápida para el desayuno.', 15, 'Fácil', 2, 'nido-breakfast-demo', 'https://images.pexels.com/photos/34871729/pexels-photo-34871729.jpeg?auto=compress&cs=tinysrgb&w=1200'),
                    ('71600000-0000-0000-0000-000000000002', 'Licuado de banana, avena y dulce de leche', 'Licuado cremoso de banana, leche entera, avena y dulce de leche, perfumado con un toque de vainilla.', 5, 'Fácil', 2, 'nido-breakfast-demo', 'https://images.unsplash.com/photo-1685967836529-b0e8d6938227?q=80&w=387&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D');
                """);

            migrationBuilder.Sql("""
                INSERT INTO ingredientes_receta (id, receta_id, producto_id, nombre_ingrediente, cantidad, unidad)
                VALUES
                    ('71610000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000001', '95197678-16f2-4176-8892-b4c4854489ab', 'Huevos Blancos', 4, 'unidad'),
                    ('71610000-0000-0000-0000-000000000002', '71600000-0000-0000-0000-000000000001', 'f66f1c2d-c89e-4f71-bd1f-810d931de1a3', 'Espinaca', 100, 'g'),
                    ('71610000-0000-0000-0000-000000000003', '71600000-0000-0000-0000-000000000001', 'fa00f939-6cbd-4d8d-adb4-7ffbee1de387', 'Queso Cremoso', 80, 'g'),
                    ('71610000-0000-0000-0000-000000000004', '71600000-0000-0000-0000-000000000001', NULL, 'pan de molde', 4, 'unidad'),
                    ('71610000-0000-0000-0000-000000000005', '71600000-0000-0000-0000-000000000001', '43a8d031-c103-4eaa-881c-f3a8a13821ea', 'Manteca', 1, 'cda'),
                    ('71610000-0000-0000-0000-000000000006', '71600000-0000-0000-0000-000000000001', '35eb75e0-d1cc-4dc2-9df9-ae9d552200a0', 'Sal Fina', 1, 'pizca'),
                    ('71610000-0000-0000-0000-000000000007', '71600000-0000-0000-0000-000000000001', 'ee42a369-8cc2-4da9-8bb0-fbcc75dbf635', 'Pimienta Negra', 1, 'pizca'),
                    ('71610000-0000-0000-0000-000000000008', '71600000-0000-0000-0000-000000000002', '0b3e7876-82e6-4c48-b9ab-e642f1113758', 'Banana', 2, 'unidad'),
                    ('71610000-0000-0000-0000-000000000009', '71600000-0000-0000-0000-000000000002', '0b08b9a8-553f-4b2c-83c4-70bbb7cf5a57', 'Leche Entera', 300, 'ml'),
                    ('71610000-0000-0000-0000-000000000010', '71600000-0000-0000-0000-000000000002', 'd0e67137-1fa0-4fa0-addf-73821344a394', 'Dulce de Leche', 2, 'cda'),
                    ('71610000-0000-0000-0000-000000000011', '71600000-0000-0000-0000-000000000002', 'be113f95-cd2f-4596-b21d-4557312e1bf0', 'Avena', 3, 'cda'),
                    ('71610000-0000-0000-0000-000000000012', '71600000-0000-0000-0000-000000000002', '50f1b496-187b-4683-be33-3e6918a7f4ad', 'esencia de vainilla', 1, 'cdta');
                """);

            migrationBuilder.Sql("""
                INSERT INTO pasos_receta (id, receta_id, orden, descripcion)
                VALUES
                    ('71630000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000001', 1, 'Lavar la espinaca, escurrirla bien y cortar el queso cremoso en cubos pequeños.'),
                    ('71630000-0000-0000-0000-000000000002', '71600000-0000-0000-0000-000000000001', 2, 'Tostar el pan hasta que quede dorado y reservarlo.'),
                    ('71630000-0000-0000-0000-000000000003', '71600000-0000-0000-0000-000000000001', 3, 'Batir los huevos con la sal fina y la pimienta negra.'),
                    ('71630000-0000-0000-0000-000000000004', '71600000-0000-0000-0000-000000000001', 4, 'Derretir la manteca en una sartén a fuego medio y saltear la espinaca hasta que reduzca su volumen.'),
                    ('71630000-0000-0000-0000-000000000005', '71600000-0000-0000-0000-000000000001', 5, 'Incorporar los huevos batidos y revolver suavemente hasta que cuajen; sumar el queso al final para que se funda.'),
                    ('71630000-0000-0000-0000-000000000006', '71600000-0000-0000-0000-000000000001', 6, 'Distribuir el revuelto sobre las tostadas y servir de inmediato.'),
                    ('71630000-0000-0000-0000-000000000007', '71600000-0000-0000-0000-000000000002', 1, 'Pelar las bananas y cortarlas en rodajas.'),
                    ('71630000-0000-0000-0000-000000000008', '71600000-0000-0000-0000-000000000002', 2, 'Colocar en la licuadora la banana, la leche entera, el dulce de leche, la avena y la esencia de vainilla.'),
                    ('71630000-0000-0000-0000-000000000009', '71600000-0000-0000-0000-000000000002', 3, 'Licuar hasta obtener una preparación homogénea y cremosa.'),
                    ('71630000-0000-0000-0000-000000000010', '71600000-0000-0000-0000-000000000002', 4, 'Probar la consistencia y agregar un chorrito adicional de leche si se prefiere más liviano.'),
                    ('71630000-0000-0000-0000-000000000011', '71600000-0000-0000-0000-000000000002', 5, 'Servir frío en dos vasos.');
                """);

            migrationBuilder.Sql("""
                INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas)
                VALUES
                    ('71620000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000001', 385, 22, 22, 23),
                    ('71620000-0000-0000-0000-000000000002', '71600000-0000-0000-0000-000000000002', 275, 9, 50, 5);
                """);

            migrationBuilder.Sql("""
                INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido)
                VALUES
                    ('71640000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000001', 'Horno/Cocina'),
                    ('71640000-0000-0000-0000-000000000002', '71600000-0000-0000-0000-000000000002', 'Licuadora');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Preserve complete seeded recipes whenever users or non-seed children reference them.
                DO $$
                DECLARE
                    seed_receta_id uuid;
                BEGIN
                    FOR seed_receta_id IN
                        SELECT id
                        FROM recetas
                        WHERE id IN ('71600000-0000-0000-0000-000000000001', '71600000-0000-0000-0000-000000000002')
                          AND fuente_id = 'nido-breakfast-demo'
                        FOR UPDATE
                    LOOP
                        IF NOT EXISTS (
                            SELECT 1 FROM recetas_cocinadas WHERE receta_id = seed_receta_id
                            UNION ALL SELECT 1 FROM recetas_guardadas_hogar WHERE receta_id = seed_receta_id
                            UNION ALL SELECT 1 FROM resenias_receta WHERE receta_id = seed_receta_id
                            UNION ALL SELECT 1 FROM planificador_item WHERE receta_id = seed_receta_id
                            UNION ALL SELECT 1 FROM notas_receta WHERE receta_id = seed_receta_id
                            UNION ALL SELECT 1 FROM ingredientes_receta
                                WHERE receta_id = seed_receta_id
                                  AND id <> ALL (ARRAY[
                                      '71610000-0000-0000-0000-000000000001'::uuid, '71610000-0000-0000-0000-000000000002'::uuid,
                                      '71610000-0000-0000-0000-000000000003'::uuid, '71610000-0000-0000-0000-000000000004'::uuid,
                                      '71610000-0000-0000-0000-000000000005'::uuid, '71610000-0000-0000-0000-000000000006'::uuid,
                                      '71610000-0000-0000-0000-000000000007'::uuid, '71610000-0000-0000-0000-000000000008'::uuid,
                                      '71610000-0000-0000-0000-000000000009'::uuid, '71610000-0000-0000-0000-000000000010'::uuid,
                                      '71610000-0000-0000-0000-000000000011'::uuid, '71610000-0000-0000-0000-000000000012'::uuid])
                            UNION ALL SELECT 1 FROM pasos_receta
                                WHERE receta_id = seed_receta_id
                                  AND id <> ALL (ARRAY[
                                      '71630000-0000-0000-0000-000000000001'::uuid, '71630000-0000-0000-0000-000000000002'::uuid,
                                      '71630000-0000-0000-0000-000000000003'::uuid, '71630000-0000-0000-0000-000000000004'::uuid,
                                      '71630000-0000-0000-0000-000000000005'::uuid, '71630000-0000-0000-0000-000000000006'::uuid,
                                      '71630000-0000-0000-0000-000000000007'::uuid, '71630000-0000-0000-0000-000000000008'::uuid,
                                      '71630000-0000-0000-0000-000000000009'::uuid, '71630000-0000-0000-0000-000000000010'::uuid,
                                      '71630000-0000-0000-0000-000000000011'::uuid])
                            UNION ALL SELECT 1 FROM info_nutricional_receta
                                WHERE receta_id = seed_receta_id
                                  AND id <> ALL (ARRAY['71620000-0000-0000-0000-000000000001'::uuid, '71620000-0000-0000-0000-000000000002'::uuid])
                            UNION ALL SELECT 1 FROM receta_electrodomestico
                                WHERE receta_id = seed_receta_id
                                  AND id <> ALL (ARRAY['71640000-0000-0000-0000-000000000001'::uuid, '71640000-0000-0000-0000-000000000002'::uuid])
                        ) THEN
                            DELETE FROM receta_electrodomestico
                            WHERE receta_id = seed_receta_id
                              AND id IN ('71640000-0000-0000-0000-000000000001', '71640000-0000-0000-0000-000000000002');

                            DELETE FROM info_nutricional_receta
                            WHERE receta_id = seed_receta_id
                              AND id IN ('71620000-0000-0000-0000-000000000001', '71620000-0000-0000-0000-000000000002');

                            DELETE FROM pasos_receta
                            WHERE receta_id = seed_receta_id
                              AND id IN (
                                  '71630000-0000-0000-0000-000000000001', '71630000-0000-0000-0000-000000000002',
                                  '71630000-0000-0000-0000-000000000003', '71630000-0000-0000-0000-000000000004',
                                  '71630000-0000-0000-0000-000000000005', '71630000-0000-0000-0000-000000000006',
                                  '71630000-0000-0000-0000-000000000007', '71630000-0000-0000-0000-000000000008',
                                  '71630000-0000-0000-0000-000000000009', '71630000-0000-0000-0000-000000000010',
                                  '71630000-0000-0000-0000-000000000011');

                            DELETE FROM ingredientes_receta
                            WHERE receta_id = seed_receta_id
                              AND id IN (
                                  '71610000-0000-0000-0000-000000000001', '71610000-0000-0000-0000-000000000002',
                                  '71610000-0000-0000-0000-000000000003', '71610000-0000-0000-0000-000000000004',
                                  '71610000-0000-0000-0000-000000000005', '71610000-0000-0000-0000-000000000006',
                                  '71610000-0000-0000-0000-000000000007', '71610000-0000-0000-0000-000000000008',
                                  '71610000-0000-0000-0000-000000000009', '71610000-0000-0000-0000-000000000010',
                                  '71610000-0000-0000-0000-000000000011', '71610000-0000-0000-0000-000000000012');

                            DELETE FROM recetas
                            WHERE id = seed_receta_id
                              AND fuente_id = 'nido-breakfast-demo';
                        END IF;
                    END LOOP;
                END $$;
                """);
        }
    }
}
