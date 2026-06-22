using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260617162000_SeedRecetasSpoonacularLote04")]
    public partial class SeedRecetasSpoonacularLote04 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url)
                VALUES
                    ('24000000-0000-0000-0000-000000000001', 'Pollo BBQ', 'Piezas de pollo doradas con una salsa simple de ketchup, azucar rubia, mostaza, soja y salsa inglesa.', 45, 'Media', 4, 'spoonacular-634476', 'https://img.spoonacular.com/recipes/634476-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000002', 'Pollo balti con arroz', 'Pollo salteado con morrones, cebolla y salsa balti, servido sobre arroz blanco.', 45, 'Media', 5, 'spoonacular-633959', 'https://img.spoonacular.com/recipes/633959-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000003', 'Carne braseada con soja y jengibre', 'Carne de roast beef o aguja braseada en caldo con salsa de soja, jengibre, ajo y jerez.', 45, 'Media', 4, 'spoonacular-635796', 'https://img.spoonacular.com/recipes/635796-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000004', 'Estofado de carne en coccion lenta', 'Estofado de carne con papas, zanahoria, apio, cebolla y caldo, cocido lentamente hasta quedar tierno.', 490, 'Dificil', 6, 'spoonacular-715446', 'https://img.spoonacular.com/recipes/715446-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000005', 'Tortilla espanola de papa y pollo', 'Tortilla al horno con papa, huevo, pollo cocido, perejil y aceite de oliva.', 30, 'Facil', 4, 'spoonacular-1095794', 'https://img.spoonacular.com/recipes/1095794-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000006', 'Tortilla horneada de acelga y hongos', 'Preparacion horneada con acelga, hongos, huevo, leche, harina de garbanzo y queso provolone.', 45, 'Media', 4, 'spoonacular-662668', 'https://img.spoonacular.com/recipes/662668-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000007', 'Tortilla de verduras', 'Tortilla de huevo rellena con echalote, ajo, hongos, tomate cherry, espinaca, albahaca y queso.', 45, 'Media', 2, 'spoonacular-650239', 'https://img.spoonacular.com/recipes/650239-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000008', 'Curry etiope de lentejas', 'Curry de lentejas con coliflor, arvejas, tomate, cebolla, ajo y especias.', 75, 'Dificil', 6, 'spoonacular-642468', 'https://img.spoonacular.com/recipes/642468-556x370.jpg'),
                    ('24000000-0000-0000-0000-000000000009', 'Guiso de garbanzos y coliflor', 'Guiso especiado de garbanzos con coliflor, batata, tomate y arroz integral.', 45, 'Media', 4, 'spoonacular-637297', NULL)
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO ingredientes_receta (id, receta_id, producto_id, nombre_ingrediente, cantidad, unidad)
                VALUES
                    ('24000001-0000-0000-0000-000000000001', '24000000-0000-0000-0000-000000000001', NULL, 'Azucar rubia', 3, 'cda'),
                    ('24000001-0000-0000-0000-000000000002', '24000000-0000-0000-0000-000000000001', NULL, 'Ketchup', 0.25, 'taza'),
                    ('24000001-0000-0000-0000-000000000003', '24000000-0000-0000-0000-000000000001', NULL, 'Piezas de pollo', 1134, 'g'),
                    ('24000001-0000-0000-0000-000000000004', '24000000-0000-0000-0000-000000000001', NULL, 'Mostaza seca', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000005', '24000000-0000-0000-0000-000000000001', NULL, 'Salsa de soja', 2, 'cda'),
                    ('24000001-0000-0000-0000-000000000006', '24000000-0000-0000-0000-000000000001', NULL, 'Salsa inglesa', 2, 'cda'),

                    ('24000001-0000-0000-0000-000000000007', '24000000-0000-0000-0000-000000000002', NULL, 'Pechugas de pollo sin piel en cubos', 4, 'unidad'),
                    ('24000001-0000-0000-0000-000000000008', '24000000-0000-0000-0000-000000000002', NULL, 'Arroz blanco cocido', 2, 'taza'),
                    ('24000001-0000-0000-0000-000000000009', '24000000-0000-0000-0000-000000000002', NULL, 'Cilantro fresco picado', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000010', '24000000-0000-0000-0000-000000000002', NULL, 'Morron verde picado', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000011', '24000000-0000-0000-0000-000000000002', NULL, 'Aceite para cocinar', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000012', '24000000-0000-0000-0000-000000000002', NULL, 'Cebolla grande picada', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000013', '24000000-0000-0000-0000-000000000002', NULL, 'Morron rojo picado', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000014', '24000000-0000-0000-0000-000000000002', NULL, 'Salsa balti', NULL, NULL),

                    ('24000001-0000-0000-0000-000000000015', '24000000-0000-0000-0000-000000000003', NULL, 'Cebolla de verdeo', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000016', '24000000-0000-0000-0000-000000000003', NULL, 'Jerez seco', 0.25, 'taza'),
                    ('24000001-0000-0000-0000-000000000017', '24000000-0000-0000-0000-000000000003', NULL, 'Azucar', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000018', '24000000-0000-0000-0000-000000000003', NULL, 'Ajo picado', 1, 'diente'),
                    ('24000001-0000-0000-0000-000000000019', '24000000-0000-0000-0000-000000000003', NULL, 'Sal', 0.5, 'cdta'),
                    ('24000001-0000-0000-0000-000000000020', '24000000-0000-0000-0000-000000000003', NULL, 'Pimienta', 1, 'pizca'),
                    ('24000001-0000-0000-0000-000000000021', '24000000-0000-0000-0000-000000000003', NULL, 'Aceite de mani', 2.5, 'cdta'),
                    ('24000001-0000-0000-0000-000000000022', '24000000-0000-0000-0000-000000000003', NULL, 'Carne de aguja vacuna', 907, 'g'),
                    ('24000001-0000-0000-0000-000000000023', '24000000-0000-0000-0000-000000000003', NULL, 'Salsa de soja', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000024', '24000000-0000-0000-0000-000000000003', NULL, 'Caldo de pollo', 3, 'taza'),
                    ('24000001-0000-0000-0000-000000000025', '24000000-0000-0000-0000-000000000003', NULL, 'Jengibre fresco picado', 4, 'rodaja'),

                    ('24000001-0000-0000-0000-000000000026', '24000000-0000-0000-0000-000000000004', NULL, 'Caldo de carne', 411, 'ml'),
                    ('24000001-0000-0000-0000-000000000027', '24000000-0000-0000-0000-000000000004', NULL, 'Zanahorias grandes picadas', 2, 'unidad'),
                    ('24000001-0000-0000-0000-000000000028', '24000000-0000-0000-0000-000000000004', NULL, 'Tallos de apio picados', 2, 'unidad'),
                    ('24000001-0000-0000-0000-000000000029', '24000000-0000-0000-0000-000000000004', NULL, 'Sopa crema de hongos', 737, 'g'),
                    ('24000001-0000-0000-0000-000000000030', '24000000-0000-0000-0000-000000000004', NULL, 'Cebolla de verdeo picada', 3, 'unidad'),
                    ('24000001-0000-0000-0000-000000000031', '24000000-0000-0000-0000-000000000004', NULL, 'Papas rojas chicas', 10, 'unidad'),
                    ('24000001-0000-0000-0000-000000000032', '24000000-0000-0000-0000-000000000004', NULL, 'Cebolla chica picada', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000033', '24000000-0000-0000-0000-000000000004', NULL, 'Condimento liquido para carne', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000034', '24000000-0000-0000-0000-000000000004', NULL, 'Carne para estofado', 907, 'g'),
                    ('24000001-0000-0000-0000-000000000035', '24000000-0000-0000-0000-000000000004', NULL, 'Agua', 2, 'taza'),

                    ('24000001-0000-0000-0000-000000000036', '24000000-0000-0000-0000-000000000005', NULL, 'Manteca sin sal', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000037', '24000000-0000-0000-0000-000000000005', NULL, 'Aceite de oliva', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000038', '24000000-0000-0000-0000-000000000005', NULL, 'Papas peladas en cubos', 2, 'unidad'),
                    ('24000001-0000-0000-0000-000000000039', '24000000-0000-0000-0000-000000000005', NULL, 'Pechuga de pollo cocida', 1, 'taza'),
                    ('24000001-0000-0000-0000-000000000040', '24000000-0000-0000-0000-000000000005', NULL, 'Huevos', 4, 'unidad'),
                    ('24000001-0000-0000-0000-000000000041', '24000000-0000-0000-0000-000000000005', NULL, 'Perejil picado', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000042', '24000000-0000-0000-0000-000000000005', NULL, 'Sal y pimienta', NULL, NULL),

                    ('24000001-0000-0000-0000-000000000043', '24000000-0000-0000-0000-000000000006', NULL, 'Hongos cremini fileteados', 2, 'taza'),
                    ('24000001-0000-0000-0000-000000000044', '24000000-0000-0000-0000-000000000006', NULL, 'Huevos batidos', 2, 'unidad'),
                    ('24000001-0000-0000-0000-000000000045', '24000000-0000-0000-0000-000000000006', NULL, 'Harina de garbanzo', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000046', '24000000-0000-0000-0000-000000000006', NULL, 'Ajo picado', 1, 'diente'),
                    ('24000001-0000-0000-0000-000000000047', '24000000-0000-0000-0000-000000000006', NULL, 'Margarina', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000048', '24000000-0000-0000-0000-000000000006', NULL, 'Aceite de oliva extra virgen', 2, 'cda'),
                    ('24000001-0000-0000-0000-000000000049', '24000000-0000-0000-0000-000000000006', NULL, 'Queso provolone rallado', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000050', '24000000-0000-0000-0000-000000000006', NULL, 'Cebolla morada en trozos', 0.25, 'taza'),
                    ('24000001-0000-0000-0000-000000000051', '24000000-0000-0000-0000-000000000006', NULL, 'Romero', 0.25, 'cdta'),
                    ('24000001-0000-0000-0000-000000000052', '24000000-0000-0000-0000-000000000006', NULL, 'Sal y pimienta', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000053', '24000000-0000-0000-0000-000000000006', NULL, 'Leche descremada', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000054', '24000000-0000-0000-0000-000000000006', NULL, 'Acelga picada gruesa', 2, 'taza'),
                    ('24000001-0000-0000-0000-000000000055', '24000000-0000-0000-0000-000000000006', NULL, 'Tomillo', 0.25, 'cdta'),

                    ('24000001-0000-0000-0000-000000000056', '24000000-0000-0000-0000-000000000007', NULL, 'Echalote picado', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000057', '24000000-0000-0000-0000-000000000007', NULL, 'Ajo picado', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000058', '24000000-0000-0000-0000-000000000007', NULL, 'Hongos fileteados', 4, 'unidad'),
                    ('24000001-0000-0000-0000-000000000059', '24000000-0000-0000-0000-000000000007', NULL, 'Tomates cherry fileteados', 8, 'unidad'),
                    ('24000001-0000-0000-0000-000000000060', '24000000-0000-0000-0000-000000000007', NULL, 'Albahaca fresca picada', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000061', '24000000-0000-0000-0000-000000000007', NULL, 'Espinaca fresca picada', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000062', '24000000-0000-0000-0000-000000000007', NULL, 'Huevos batidos', 4, 'unidad'),
                    ('24000001-0000-0000-0000-000000000063', '24000000-0000-0000-0000-000000000007', NULL, 'Queso blanco', 0.5, 'taza'),
                    ('24000001-0000-0000-0000-000000000064', '24000000-0000-0000-0000-000000000007', NULL, 'Aceite de oliva', NULL, NULL),

                    ('24000001-0000-0000-0000-000000000065', '24000000-0000-0000-0000-000000000008', NULL, 'Amchar masala', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000066', '24000000-0000-0000-0000-000000000008', NULL, 'Lentejas marrones', 1, 'taza'),
                    ('24000001-0000-0000-0000-000000000067', '24000000-0000-0000-0000-000000000008', NULL, 'Tomates triturados', 1, 'lata'),
                    ('24000001-0000-0000-0000-000000000068', '24000000-0000-0000-0000-000000000008', NULL, 'Coliflor en bocados', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000069', '24000000-0000-0000-0000-000000000008', NULL, 'Ajo picado', 2, 'diente'),
                    ('24000001-0000-0000-0000-000000000070', '24000000-0000-0000-0000-000000000008', NULL, 'Cebolla en cubos', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000071', '24000000-0000-0000-0000-000000000008', NULL, 'Arvejas congeladas', 2, 'taza'),
                    ('24000001-0000-0000-0000-000000000072', '24000000-0000-0000-0000-000000000008', NULL, 'Yogur natural opcional', 0.25, 'taza'),
                    ('24000001-0000-0000-0000-000000000073', '24000000-0000-0000-0000-000000000008', NULL, 'Berbere molido', 2, 'cda'),
                    ('24000001-0000-0000-0000-000000000074', '24000000-0000-0000-0000-000000000008', NULL, 'Pure de tomate', 1, 'lata'),
                    ('24000001-0000-0000-0000-000000000075', '24000000-0000-0000-0000-000000000008', NULL, 'Aceite vegetal', 2, 'cda'),

                    ('24000001-0000-0000-0000-000000000076', '24000000-0000-0000-0000-000000000009', NULL, 'Aceite de oliva', 1, 'cda'),
                    ('24000001-0000-0000-0000-000000000077', '24000000-0000-0000-0000-000000000009', NULL, 'Arroz integral', 240, 'g'),
                    ('24000001-0000-0000-0000-000000000078', '24000000-0000-0000-0000-000000000009', NULL, 'Agua para el arroz', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000079', '24000000-0000-0000-0000-000000000009', NULL, 'Ajo machacado', 5, 'diente'),
                    ('24000001-0000-0000-0000-000000000080', '24000000-0000-0000-0000-000000000009', NULL, 'Curcuma', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000081', '24000000-0000-0000-0000-000000000009', NULL, 'Sal y pimienta', NULL, NULL),
                    ('24000001-0000-0000-0000-000000000082', '24000000-0000-0000-0000-000000000009', NULL, 'Cebolla picada fina', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000083', '24000000-0000-0000-0000-000000000009', NULL, 'Tomates picados en lata', 400, 'g'),
                    ('24000001-0000-0000-0000-000000000084', '24000000-0000-0000-0000-000000000009', NULL, 'Pure de tomate', 2, 'cda'),
                    ('24000001-0000-0000-0000-000000000085', '24000000-0000-0000-0000-000000000009', NULL, 'Jengibre fresco rallado', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000086', '24000000-0000-0000-0000-000000000009', NULL, 'Chile fresco picado', 0.5, 'unidad'),
                    ('24000001-0000-0000-0000-000000000087', '24000000-0000-0000-0000-000000000009', NULL, 'Canela', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000088', '24000000-0000-0000-0000-000000000009', NULL, 'Garam masala', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000089', '24000000-0000-0000-0000-000000000009', NULL, 'Pimenton', 1, 'cdta'),
                    ('24000001-0000-0000-0000-000000000090', '24000000-0000-0000-0000-000000000009', NULL, 'Batata en cubos', 1, 'unidad'),
                    ('24000001-0000-0000-0000-000000000091', '24000000-0000-0000-0000-000000000009', NULL, 'Jugo de limon', 0.5, 'unidad'),
                    ('24000001-0000-0000-0000-000000000092', '24000000-0000-0000-0000-000000000009', NULL, 'Agua', 240, 'ml'),
                    ('24000001-0000-0000-0000-000000000093', '24000000-0000-0000-0000-000000000009', NULL, 'Coliflor en bocados', 0.5, 'unidad'),
                    ('24000001-0000-0000-0000-000000000094', '24000000-0000-0000-0000-000000000009', NULL, 'Garbanzos cocidos', 400, 'g')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO pasos_receta (id, receta_id, orden, descripcion)
                VALUES
                    ('24000003-0000-0000-0000-000000000001', '24000000-0000-0000-0000-000000000001', 1, 'Mezclar el azucar rubia, el ketchup, la mostaza seca, la salsa de soja y la salsa inglesa.'),
                    ('24000003-0000-0000-0000-000000000002', '24000000-0000-0000-0000-000000000001', 2, 'Pintar el pollo con la mitad de la salsa y dorarlo bajo el grill del horno durante 20 minutos.'),
                    ('24000003-0000-0000-0000-000000000003', '24000000-0000-0000-0000-000000000001', 3, 'Dar vuelta el pollo, pintar con el resto de la salsa y cocinar 20 minutos mas.'),

                    ('24000003-0000-0000-0000-000000000004', '24000000-0000-0000-0000-000000000002', 1, 'Calentar aceite en una sarten pesada o wok a fuego medio-alto y cocinar el pollo hasta que no queden partes rosadas.'),
                    ('24000003-0000-0000-0000-000000000005', '24000000-0000-0000-0000-000000000002', 2, 'Agregar los morrones y la cebolla picados. Saltear de 3 a 5 minutos.'),
                    ('24000003-0000-0000-0000-000000000006', '24000000-0000-0000-0000-000000000002', 3, 'Incorporar la salsa balti y cocinar a fuego suave unos 5 minutos.'),
                    ('24000003-0000-0000-0000-000000000007', '24000000-0000-0000-0000-000000000002', 4, 'Servir sobre arroz blanco cocido y terminar con cilantro fresco picado.'),

                    ('24000003-0000-0000-0000-000000000008', '24000000-0000-0000-0000-000000000003', 1, 'Cortar la parte verde de la cebolla de verdeo en trozos.'),
                    ('24000003-0000-0000-0000-000000000009', '24000000-0000-0000-0000-000000000003', 2, 'Mezclar la cebolla de verdeo, el jerez, el azucar, el ajo, la sal, la pimienta, la salsa de soja y el jengibre.'),
                    ('24000003-0000-0000-0000-000000000010', '24000000-0000-0000-0000-000000000003', 3, 'Calentar el aceite en una sarten pesada y dorar la carne rapido por todos sus lados.'),
                    ('24000003-0000-0000-0000-000000000011', '24000000-0000-0000-0000-000000000003', 4, 'Agregar la mezcla de soja y cocinar unos 3 minutos, revolviendo para integrar.'),
                    ('24000003-0000-0000-0000-000000000012', '24000000-0000-0000-0000-000000000003', 5, 'Calentar el caldo, sumarlo a la carne y cocinar tapado de 90 minutos a 2 horas, hasta que este tierna.'),
                    ('24000003-0000-0000-0000-000000000013', '24000000-0000-0000-0000-000000000003', 6, 'Cortar la carne en rodajas y servir caliente o fria con su salsa.'),

                    ('24000003-0000-0000-0000-000000000014', '24000000-0000-0000-0000-000000000004', 1, 'Calentar la olla de coccion lenta en temperatura baja.'),
                    ('24000003-0000-0000-0000-000000000015', '24000000-0000-0000-0000-000000000004', 2, 'Mezclar la sopa crema de hongos, el condimento liquido, el agua y el caldo de carne.'),
                    ('24000003-0000-0000-0000-000000000016', '24000000-0000-0000-0000-000000000004', 3, 'Agregar la carne, las papas, las cebollas, las zanahorias, el apio y la cebolla de verdeo.'),
                    ('24000003-0000-0000-0000-000000000017', '24000000-0000-0000-0000-000000000004', 4, 'Mezclar bien, tapar y cocinar en bajo durante 8 horas. Ajustar sal y pimienta si hace falta.'),

                    ('24000003-0000-0000-0000-000000000018', '24000000-0000-0000-0000-000000000005', 1, 'Precalentar el grill del horno. Hervir las papas en agua con sal hasta que esten tiernas pero firmes.'),
                    ('24000003-0000-0000-0000-000000000019', '24000000-0000-0000-0000-000000000005', 2, 'Escurrir las papas. Batir los huevos con sal y pimienta.'),
                    ('24000003-0000-0000-0000-000000000020', '24000000-0000-0000-0000-000000000005', 3, 'En una sarten apta para horno, derretir la manteca con el aceite de oliva y calentar el pollo.'),
                    ('24000003-0000-0000-0000-000000000021', '24000000-0000-0000-0000-000000000005', 4, 'Agregar las papas y cocinar con el pollo durante 3 minutos.'),
                    ('24000003-0000-0000-0000-000000000022', '24000000-0000-0000-0000-000000000005', 5, 'Sumar los huevos y el perejil. Cocinar hasta que la base y los bordes empiecen a cuajar.'),
                    ('24000003-0000-0000-0000-000000000023', '24000000-0000-0000-0000-000000000005', 6, 'Llevar bajo el grill unos 6 minutos, hasta dorar y terminar de cocinar el huevo.'),
                    ('24000003-0000-0000-0000-000000000024', '24000000-0000-0000-0000-000000000005', 7, 'Servir en porciones con una ensalada verde.'),

                    ('24000003-0000-0000-0000-000000000025', '24000000-0000-0000-0000-000000000006', 1, 'Precalentar el horno a temperatura alta.'),
                    ('24000003-0000-0000-0000-000000000026', '24000000-0000-0000-0000-000000000006', 2, 'Calentar aceite de oliva en una sarten y cocinar la cebolla y los hongos hasta que esten tiernos.'),
                    ('24000003-0000-0000-0000-000000000027', '24000000-0000-0000-0000-000000000006', 3, 'Agregar la acelga, el ajo, el romero, el tomillo, sal y pimienta. Cocinar unos minutos y retirar del fuego.'),
                    ('24000003-0000-0000-0000-000000000028', '24000000-0000-0000-0000-000000000006', 4, 'Colocar la margarina en una fuente y llevarla al horno hasta que se derrita.'),
                    ('24000003-0000-0000-0000-000000000029', '24000000-0000-0000-0000-000000000006', 5, 'Batir la harina de garbanzo con la leche y los huevos. Verter en la fuente y hornear de 12 a 14 minutos, hasta que infle y dore apenas.'),
                    ('24000003-0000-0000-0000-000000000030', '24000000-0000-0000-0000-000000000006', 6, 'Cubrir con la mezcla de acelga y hongos, espolvorear el provolone y hornear unos 10 minutos mas, hasta fundir el queso.'),

                    ('24000003-0000-0000-0000-000000000031', '24000000-0000-0000-0000-000000000007', 1, 'Precalentar el horno bajo para mantener caliente la primera tortilla.'),
                    ('24000003-0000-0000-0000-000000000032', '24000000-0000-0000-0000-000000000007', 2, 'Saltear el echalote y el ajo con aceite de oliva. Agregar los hongos hasta que esten tiernos.'),
                    ('24000003-0000-0000-0000-000000000033', '24000000-0000-0000-0000-000000000007', 3, 'Sumar los tomates y la espinaca. Retirar del fuego.'),
                    ('24000003-0000-0000-0000-000000000034', '24000000-0000-0000-0000-000000000007', 4, 'Verter una capa de huevo batido en una sarten chica. Cuando casi este cocida, agregar albahaca, queso y parte de las verduras en un lado.'),
                    ('24000003-0000-0000-0000-000000000035', '24000000-0000-0000-0000-000000000007', 5, 'Doblar la tortilla sobre el relleno y reservar caliente. Repetir con la segunda porcion.'),

                    ('24000003-0000-0000-0000-000000000036', '24000000-0000-0000-0000-000000000008', 1, 'Calentar el aceite en una olla grande a fuego medio.'),
                    ('24000003-0000-0000-0000-000000000037', '24000000-0000-0000-0000-000000000008', 2, 'Agregar la cebolla y saltear hasta que este translucida.'),
                    ('24000003-0000-0000-0000-000000000038', '24000000-0000-0000-0000-000000000008', 3, 'Sumar el ajo picado y cocinar un minuto mas.'),
                    ('24000003-0000-0000-0000-000000000039', '24000000-0000-0000-0000-000000000008', 4, 'Incorporar la coliflor, las arvejas y las lentejas. Condimentar con amchar masala y berbere, y saltear 5 minutos.'),
                    ('24000003-0000-0000-0000-000000000040', '24000000-0000-0000-0000-000000000008', 5, 'Agregar los tomates triturados y el pure de tomate. Mezclar bien.'),
                    ('24000003-0000-0000-0000-000000000041', '24000000-0000-0000-0000-000000000008', 6, 'Sumar unas 2 tazas de agua, llevar a hervor, bajar el fuego, tapar y cocinar cerca de 1 hora, hasta que las lentejas esten tiernas.'),
                    ('24000003-0000-0000-0000-000000000042', '24000000-0000-0000-0000-000000000008', 7, 'Mezclar el yogur natural opcional y servir enseguida.'),

                    ('24000003-0000-0000-0000-000000000043', '24000000-0000-0000-0000-000000000009', 1, 'Calentar aceite de oliva en una cacerola grande. Machacar ajo y saltearlo con curcuma, sal y pimienta durante 1 minuto.'),
                    ('24000003-0000-0000-0000-000000000044', '24000000-0000-0000-0000-000000000009', 2, 'Agregar el arroz integral y saltear de 4 a 5 minutos. Sumar agua, llevar a hervor y cocinar a fuego bajo unos 30 minutos.'),
                    ('24000003-0000-0000-0000-000000000045', '24000000-0000-0000-0000-000000000009', 3, 'En otra cacerola, saltear la cebolla hasta dorar. Agregar tomate, pure de tomate, chile, jengibre, ajo y especias.'),
                    ('24000003-0000-0000-0000-000000000046', '24000000-0000-0000-0000-000000000009', 4, 'Sumar la batata, el jugo de limon y el agua. Llevar a hervor y cocinar de 30 a 35 minutos.'),
                    ('24000003-0000-0000-0000-000000000047', '24000000-0000-0000-0000-000000000009', 5, 'Agregar la coliflor 10 minutos antes de terminar y los garbanzos escurridos 5 minutos antes de apagar el fuego.'),
                    ('24000003-0000-0000-0000-000000000048', '24000000-0000-0000-0000-000000000009', 6, 'Ajustar sal y pimienta, y servir con hierbas frescas si hay disponibles.')
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas)
                VALUES
                    ('24000002-0000-0000-0000-000000000001', '24000000-0000-0000-0000-000000000001', 478.31, 37.10, 15.21, 29.24),
                    ('24000002-0000-0000-0000-000000000002', '24000000-0000-0000-0000-000000000002', 519.89, 25.24, 64.51, 16.97),
                    ('24000002-0000-0000-0000-000000000003', '24000000-0000-0000-0000-000000000003', 530.82, 51.20, 9.84, 30.74),
                    ('24000002-0000-0000-0000-000000000004', '24000000-0000-0000-0000-000000000004', 433.82, 44.24, 40.53, 11.63),
                    ('24000002-0000-0000-0000-000000000005', '24000000-0000-0000-0000-000000000005', 260.15, 18.70, 19.18, 11.90),
                    ('24000002-0000-0000-0000-000000000006', '24000000-0000-0000-0000-000000000006', 265.78, 13.20, 14.17, 17.70),
                    ('24000002-0000-0000-0000-000000000007', '24000000-0000-0000-0000-000000000007', 399.09, 20.21, 8.25, 32.24),
                    ('24000002-0000-0000-0000-000000000008', '24000000-0000-0000-0000-000000000008', 284.63, 16.03, 44.50, 6.37),
                    ('24000002-0000-0000-0000-000000000009', '24000000-0000-0000-0000-000000000009', 455.29, 13.63, 86.28, 7.76)
                ON CONFLICT (id) DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido)
                VALUES
                    ('24000004-0000-0000-0000-000000000001', '24000000-0000-0000-0000-000000000001', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000002', '24000000-0000-0000-0000-000000000002', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000003', '24000000-0000-0000-0000-000000000003', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000004', '24000000-0000-0000-0000-000000000004', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000005', '24000000-0000-0000-0000-000000000005', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000006', '24000000-0000-0000-0000-000000000006', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000007', '24000000-0000-0000-0000-000000000007', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000008', '24000000-0000-0000-0000-000000000008', 'Horno/Cocina'),
                    ('24000004-0000-0000-0000-000000000009', '24000000-0000-0000-0000-000000000009', 'Horno/Cocina')
                ON CONFLICT (id) DO NOTHING;
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM receta_electrodomestico
                WHERE receta_id IN (
                    '24000000-0000-0000-0000-000000000001',
                    '24000000-0000-0000-0000-000000000002',
                    '24000000-0000-0000-0000-000000000003',
                    '24000000-0000-0000-0000-000000000004',
                    '24000000-0000-0000-0000-000000000005',
                    '24000000-0000-0000-0000-000000000006',
                    '24000000-0000-0000-0000-000000000007',
                    '24000000-0000-0000-0000-000000000008',
                    '24000000-0000-0000-0000-000000000009'
                );

                DELETE FROM info_nutricional_receta
                WHERE receta_id IN (
                    '24000000-0000-0000-0000-000000000001',
                    '24000000-0000-0000-0000-000000000002',
                    '24000000-0000-0000-0000-000000000003',
                    '24000000-0000-0000-0000-000000000004',
                    '24000000-0000-0000-0000-000000000005',
                    '24000000-0000-0000-0000-000000000006',
                    '24000000-0000-0000-0000-000000000007',
                    '24000000-0000-0000-0000-000000000008',
                    '24000000-0000-0000-0000-000000000009'
                );

                DELETE FROM pasos_receta
                WHERE receta_id IN (
                    '24000000-0000-0000-0000-000000000001',
                    '24000000-0000-0000-0000-000000000002',
                    '24000000-0000-0000-0000-000000000003',
                    '24000000-0000-0000-0000-000000000004',
                    '24000000-0000-0000-0000-000000000005',
                    '24000000-0000-0000-0000-000000000006',
                    '24000000-0000-0000-0000-000000000007',
                    '24000000-0000-0000-0000-000000000008',
                    '24000000-0000-0000-0000-000000000009'
                );

                DELETE FROM ingredientes_receta
                WHERE receta_id IN (
                    '24000000-0000-0000-0000-000000000001',
                    '24000000-0000-0000-0000-000000000002',
                    '24000000-0000-0000-0000-000000000003',
                    '24000000-0000-0000-0000-000000000004',
                    '24000000-0000-0000-0000-000000000005',
                    '24000000-0000-0000-0000-000000000006',
                    '24000000-0000-0000-0000-000000000007',
                    '24000000-0000-0000-0000-000000000008',
                    '24000000-0000-0000-0000-000000000009'
                );

                DELETE FROM recetas
                WHERE id IN (
                    '24000000-0000-0000-0000-000000000001',
                    '24000000-0000-0000-0000-000000000002',
                    '24000000-0000-0000-0000-000000000003',
                    '24000000-0000-0000-0000-000000000004',
                    '24000000-0000-0000-0000-000000000005',
                    '24000000-0000-0000-0000-000000000006',
                    '24000000-0000-0000-0000-000000000007',
                    '24000000-0000-0000-0000-000000000008',
                    '24000000-0000-0000-0000-000000000009'
                );
            """);
        }
    }
}
