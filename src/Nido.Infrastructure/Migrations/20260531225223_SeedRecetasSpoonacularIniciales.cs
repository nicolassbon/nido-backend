using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecetasSpoonacularIniciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO categorias_producto (id, nombre, ttl_dias)
                VALUES
                    ('f1000000-0000-0000-0000-000000000001', 'Verduras', 7),
                    ('f1000000-0000-0000-0000-000000000002', 'Aceites y condimentos', 365),
                    ('f1000000-0000-0000-0000-000000000003', 'Condimentos', 365),
                    ('f1000000-0000-0000-0000-000000000004', 'Conservas', 730),
                    ('f1000000-0000-0000-0000-000000000005', 'Despensa', 365),
                    ('f1000000-0000-0000-0000-000000000006', 'Lacteos', 14)
                ON CONFLICT (id) DO NOTHING;
            """);



            migrationBuilder.Sql("""
                INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url)
                VALUES
                    ('f3000000-0000-0000-0000-000000000001', 'Pasta ratatouille', 'Pasta con verduras salteadas estilo ratatouille, alcauciles, tomate y queso parmesano. Es una preparacion completa y sabrosa para una comida principal.', 45, 'Media', 4, 'spoonacular-657933', 'https://img.spoonacular.com/recipes/657933-556x370.jpg'),
                    ('f3000000-0000-0000-0000-000000000002', 'Pasta con atun', 'Pasta cremosa con atun, arvejas, perejil, cebolla de verdeo y queso parmesano. Una receta practica para almuerzo o cena.', 45, 'Media', 4, 'spoonacular-654959', 'https://img.spoonacular.com/recipes/654959-556x370.jpg'),
                    ('f3000000-0000-0000-0000-000000000003', 'Pasta italiana con atun', 'Pasta fria o tibia con atun, morron, perejil, ajo, aji picante y jugo de limon. Una receta rapida y liviana.', 20, 'Facil', 3, 'spoonacular-648279', 'https://img.spoonacular.com/recipes/648279-556x370.jpg')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO ingredientes_receta (id, receta_id, nombre_ingrediente, producto_id, cantidad, unidad)
                VALUES
                    ('f4000000-0000-0000-0000-000000000001', 'f3000000-0000-0000-0000-000000000001', 'Cebolla amarilla grande picada', NULL, 1, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000002', 'f3000000-0000-0000-0000-000000000001', 'Aceite de oliva para saltear', NULL, NULL, NULL),
                    ('f4000000-0000-0000-0000-000000000003', 'f3000000-0000-0000-0000-000000000001', 'Sal marina y pimienta fresca', NULL, NULL, NULL),
                    ('f4000000-0000-0000-0000-000000000004', 'f3000000-0000-0000-0000-000000000001', 'Dientes de ajo picados', NULL, 4, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000005', 'f3000000-0000-0000-0000-000000000001', 'Zucchini pelado y cortado en bocados', NULL, 372, 'g'),
                    ('f4000000-0000-0000-0000-000000000006', 'f3000000-0000-0000-0000-000000000001', 'Berenjena pelada y cortado en bocados', NULL, 246, 'g'),
                    ('f4000000-0000-0000-0000-000000000007', 'f3000000-0000-0000-0000-000000000001', 'Condimento italiano', NULL, 1, 'cdta'),
                    ('f4000000-0000-0000-0000-000000000008', 'f3000000-0000-0000-0000-000000000001', 'Alcauciles en conserva escurridos y picados', NULL, 396.89, 'g'),
                    ('f4000000-0000-0000-0000-000000000009', 'f3000000-0000-0000-0000-000000000001', 'Tomates en cubos en conserva', NULL, 396.89, 'g'),
                    ('f4000000-0000-0000-0000-000000000010', 'f3000000-0000-0000-0000-000000000001', 'Spaghetti', NULL, 226.8, 'g'),
                    ('f4000000-0000-0000-0000-000000000011', 'f3000000-0000-0000-0000-000000000001', 'Queso parmesano para servir', NULL, NULL, NULL),
                    ('f4000000-0000-0000-0000-000000000012', 'f3000000-0000-0000-0000-000000000002', 'Harina', NULL, 2, 'cda'),
                    ('f4000000-0000-0000-0000-000000000013', 'f3000000-0000-0000-0000-000000000002', 'Cebolla de verdeo picada', NULL, 100, 'g'),
                    ('f4000000-0000-0000-0000-000000000014', 'f3000000-0000-0000-0000-000000000002', 'Leche descremada', NULL, 306.25, 'ml'),
                    ('f4000000-0000-0000-0000-000000000015', 'f3000000-0000-0000-0000-000000000002', 'Aceite de oliva', NULL, 2, 'cda'),
                    ('f4000000-0000-0000-0000-000000000016', 'f3000000-0000-0000-0000-000000000002', 'Cebolla picada', NULL, 2, 'cda'),
                    ('f4000000-0000-0000-0000-000000000017', 'f3000000-0000-0000-0000-000000000002', 'Queso parmesano rallado', NULL, 25, 'g'),
                    ('f4000000-0000-0000-0000-000000000018', 'f3000000-0000-0000-0000-000000000002', 'Perejil picado', NULL, 60, 'g'),
                    ('f4000000-0000-0000-0000-000000000019', 'f3000000-0000-0000-0000-000000000002', 'Pasta tubular', NULL, 226.8, 'g'),
                    ('f4000000-0000-0000-0000-000000000020', 'f3000000-0000-0000-0000-000000000002', 'Arvejas', NULL, 145, 'g'),
                    ('f4000000-0000-0000-0000-000000000021', 'f3000000-0000-0000-0000-000000000002', 'Salsa picante', NULL, 1, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000022', 'f3000000-0000-0000-0000-000000000002', 'Atun al natural', NULL, 184.27, 'g'),
                    ('f4000000-0000-0000-0000-000000000023', 'f3000000-0000-0000-0000-000000000003', 'Ajies picantes', NULL, 2, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000024', 'f3000000-0000-0000-0000-000000000003', 'Ajo picado', NULL, 1, 'cda'),
                    ('f4000000-0000-0000-0000-000000000025', 'f3000000-0000-0000-0000-000000000003', 'Pimienta negra molida', NULL, NULL, NULL),
                    ('f4000000-0000-0000-0000-000000000026', 'f3000000-0000-0000-0000-000000000003', 'Jugo de limon', NULL, 2, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000027', 'f3000000-0000-0000-0000-000000000003', 'Hojas de perejil', NULL, 30, 'g'),
                    ('f4000000-0000-0000-0000-000000000028', 'f3000000-0000-0000-0000-000000000003', 'Pasta tipo conchiglie', NULL, 250, 'g'),
                    ('f4000000-0000-0000-0000-000000000029', 'f3000000-0000-0000-0000-000000000003', 'Morron rojo', NULL, 1, 'unidad'),
                    ('f4000000-0000-0000-0000-000000000030', 'f3000000-0000-0000-0000-000000000003', 'Atun', NULL, 400, 'g')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO pasos_receta (id, receta_id, orden, descripcion)
                VALUES
                    ('f5000000-0000-0000-0000-000000000001', 'f3000000-0000-0000-0000-000000000001', 1, 'Saltear la cebolla en una sarten grande con aceite de oliva, a fuego medio-bajo, hasta que este tierna y translucida. Condimentar con sal y pimienta.'),
                    ('f5000000-0000-0000-0000-000000000002', 'f3000000-0000-0000-0000-000000000001', 2, 'Agregar el ajo y saltear hasta que suelte aroma.'),
                    ('f5000000-0000-0000-0000-000000000003', 'f3000000-0000-0000-0000-000000000001', 3, 'Agregar un poco mas de aceite de oliva, el zucchini, la berenjena y el condimento italiano. Cocinar hasta que las verduras se ablanden.'),
                    ('f5000000-0000-0000-0000-000000000004', 'f3000000-0000-0000-0000-000000000001', 4, 'Sumar los alcauciles y los tomates. Condimentar con sal y pimienta, llevar a hervor suave y cocinar entre 10 y 15 minutos.'),
                    ('f5000000-0000-0000-0000-000000000005', 'f3000000-0000-0000-0000-000000000001', 5, 'Mientras tanto, cocinar el spaghetti al dente segun las instrucciones del paquete.'),
                    ('f5000000-0000-0000-0000-000000000006', 'f3000000-0000-0000-0000-000000000001', 6, 'Mezclar la pasta con la salsa de verduras y servir con un chorrito de aceite de oliva y queso parmesano.'),
                    ('f5000000-0000-0000-0000-000000000007', 'f3000000-0000-0000-0000-000000000002', 1, 'Cocinar la pasta en una olla grande con agua hirviendo hasta que este al dente.'),
                    ('f5000000-0000-0000-0000-000000000008', 'f3000000-0000-0000-0000-000000000002', 2, 'Escurrir la pasta y devolverla a la olla caliente.'),
                    ('f5000000-0000-0000-0000-000000000009', 'f3000000-0000-0000-0000-000000000002', 3, 'Calentar aceite de oliva en una cacerola y agregar la cebolla. Saltear hasta que este transparente.'),
                    ('f5000000-0000-0000-0000-000000000010', 'f3000000-0000-0000-0000-000000000002', 4, 'Incorporar la harina, cocinar unos segundos y luego agregar la leche batiendo constantemente hasta que espese.'),
                    ('f5000000-0000-0000-0000-000000000011', 'f3000000-0000-0000-0000-000000000002', 5, 'Agregar las arvejas, el atun desmenuzado, el perejil, la cebolla de verdeo, el queso y la salsa picante.'),
                    ('f5000000-0000-0000-0000-000000000012', 'f3000000-0000-0000-0000-000000000002', 6, 'Verter la salsa sobre la pasta, mezclar suavemente y servir enseguida.'),
                    ('f5000000-0000-0000-0000-000000000013', 'f3000000-0000-0000-0000-000000000003', 1, 'Una vez cocida la pasta, escurrirla y dejarla enfriar durante un minuto.'),
                    ('f5000000-0000-0000-0000-000000000014', 'f3000000-0000-0000-0000-000000000003', 2, 'Calentar una sarten pequena a fuego medio, agregar un poco de aceite de oliva y saltear el morron rojo durante 1 o 2 minutos. Reservar.'),
                    ('f5000000-0000-0000-0000-000000000015', 'f3000000-0000-0000-0000-000000000003', 3, 'Mezclar la pasta con el morron, el atun, el perejil, el ajo, los ajies picantes y el jugo de limon.'),
                    ('f5000000-0000-0000-0000-000000000016', 'f3000000-0000-0000-0000-000000000003', 4, 'Condimentar con pimienta negra molida a gusto y servir en bowls.')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas)
                VALUES
                    ('f6000000-0000-0000-0000-000000000001', 'f3000000-0000-0000-0000-000000000001', 690.96, 24.51, 69.05, 37.37),
                    ('f6000000-0000-0000-0000-000000000002', 'f3000000-0000-0000-0000-000000000002', 422.67, 24.32, 57.66, 10.32),
                    ('f6000000-0000-0000-0000-000000000003', 'f3000000-0000-0000-0000-000000000003', 463.7, 37.7, 70.33, 2.91)
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido)
                VALUES
                    ('f7000000-0000-0000-0000-000000000001', 'f3000000-0000-0000-0000-000000000001', 'Horno/Cocina'),
                    ('f7000000-0000-0000-0000-000000000002', 'f3000000-0000-0000-0000-000000000002', 'Horno/Cocina'),
                    ('f7000000-0000-0000-0000-000000000003', 'f3000000-0000-0000-0000-000000000003', 'Horno/Cocina')
                ON CONFLICT (id) DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM receta_electrodomestico
                WHERE receta_id IN (
                    'f3000000-0000-0000-0000-000000000001',
                    'f3000000-0000-0000-0000-000000000002',
                    'f3000000-0000-0000-0000-000000000003'
                );

                DELETE FROM info_nutricional_receta
                WHERE receta_id IN (
                    'f3000000-0000-0000-0000-000000000001',
                    'f3000000-0000-0000-0000-000000000002',
                    'f3000000-0000-0000-0000-000000000003'
                );

                DELETE FROM pasos_receta
                WHERE receta_id IN (
                    'f3000000-0000-0000-0000-000000000001',
                    'f3000000-0000-0000-0000-000000000002',
                    'f3000000-0000-0000-0000-000000000003'
                );

                DELETE FROM ingredientes_receta
                WHERE receta_id IN (
                    'f3000000-0000-0000-0000-000000000001',
                    'f3000000-0000-0000-0000-000000000002',
                    'f3000000-0000-0000-0000-000000000003'
                );

                DELETE FROM recetas
                WHERE id IN (
                    'f3000000-0000-0000-0000-000000000001',
                    'f3000000-0000-0000-0000-000000000002',
                    'f3000000-0000-0000-0000-000000000003'
                );



                DELETE FROM categorias_producto
                WHERE id IN (
                    'f1000000-0000-0000-0000-000000000001',
                    'f1000000-0000-0000-0000-000000000002',
                    'f1000000-0000-0000-0000-000000000003',
                    'f1000000-0000-0000-0000-000000000004',
                    'f1000000-0000-0000-0000-000000000005',
                    'f1000000-0000-0000-0000-000000000006'
                );
            """);
        }
    }
}
