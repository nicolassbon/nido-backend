using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecetasSpoonacularLote03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql("""
INSERT INTO categorias_producto (id, nombre) VALUES
    ('aefcb56f-88d9-55b1-8b29-27855a28d151', 'Aceites'),
    ('703d0951-e89f-5fc3-a09b-c0a9b3fd66ec', 'Caldos'),
    ('6618c204-ff83-5ec5-87b7-a9abc04f2a81', 'Carnes'),
    ('db61671a-a9fa-50f6-bbff-8604d0773560', 'Condimentos'),
    ('709b31cd-0d7a-54d8-bd6c-d909f23e2603', 'Conservas'),
    ('1de21387-806d-5b47-acb3-3a00fb36bf2f', 'Frutas'),
    ('6546c5e0-9df7-506a-b48d-ed9efcab88fb', 'Huevos'),
    ('3ef708eb-8f21-5107-95a1-28bfad9e4745', 'Legumbres'),
    ('3426d21e-cac7-5df3-bfdc-a0918ccf5af6', 'Lácteos'),
    ('7862a467-732d-5ac3-91f3-da043d96de5f', 'Repostería'),
    ('d42568b3-2b2a-59e7-960a-d70ddd29c39a', 'Verduras')
ON CONFLICT (id) DO NOTHING;

INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id) VALUES
    ('28e2ce43-fbbf-5a16-9489-2b6a1d12a6ed', 'Palta', NULL, NULL, '1de21387-806d-5b47-acb3-3a00fb36bf2f'),
    ('68e7b2e7-c0e0-51aa-8d5e-67e9e0a043c9', 'Microgreens', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('25117115-de6e-5e1a-aed9-e27843729f05', 'Albahaca', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('6aad61d1-9c89-58c0-900b-90da37496211', 'Zanahoria', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('3ffb2f7a-dc80-5733-8057-8bb13f0fd92e', 'Apio', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('9a4b8c06-e990-5ddd-a086-ae4fbe718c06', 'Pechuga de pollo', NULL, NULL, '6618c204-ff83-5ec5-87b7-a9abc04f2a81'),
    ('36a01c28-a785-59b1-ab05-efa910edec5d', 'Perejil', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('7f16df6e-4613-59cb-b52d-059b6a63b362', 'Ajo', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('20da9022-c83e-5808-afc8-9717132480f9', 'Aceite de oliva', NULL, NULL, 'aefcb56f-88d9-55b1-8b29-27855a28d151'),
    ('aed0a77b-cccf-58e0-a526-55167ef1c457', 'Tomate en conserva', NULL, NULL, '709b31cd-0d7a-54d8-bd6c-d909f23e2603'),
    ('893b00f6-a841-58f9-807b-23914fafc4b8', 'Lentejas rojas', NULL, NULL, '3ef708eb-8f21-5107-95a1-28bfad9e4745'),
    ('bd4d8594-e937-55d6-ab98-3f0b86c5f7fe', 'Sal', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'),
    ('ed60b5b4-fbf0-5486-b2c8-0494132cc208', 'Pimienta', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'),
    ('0240b252-a457-52f4-94b1-87c2ddb26e49', 'Nabo', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('e57d80a2-2eac-5aca-8104-0b15e5dc3ae9', 'Caldo de verduras', NULL, NULL, '703d0951-e89f-5fc3-a09b-c0a9b3fd66ec'),
    ('12ba1936-cbc5-5fe5-9330-f8a61e469be8', 'Cebolla', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('9954f472-3d12-52b5-a7e1-916f1d69936d', 'Aceite vegetal', NULL, NULL, 'aefcb56f-88d9-55b1-8b29-27855a28d151'),
    ('9c9468de-a887-501b-aeca-025049c6faa5', 'Carne picada vacuna', NULL, NULL, '6618c204-ff83-5ec5-87b7-a9abc04f2a81'),
    ('923ae982-eb45-578b-aa49-2ffac41725c4', 'Carne picada de cerdo', NULL, NULL, '6618c204-ff83-5ec5-87b7-a9abc04f2a81'),
    ('05923054-8fe5-5bc9-8118-d25713e9e15e', 'Cebolla de verdeo', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('0bae6710-70fc-5693-9f77-7adefadd3aeb', 'Morrón', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'),
    ('de07f185-bc5d-5c15-9a16-bdce6e626553', 'Ají molido', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'),
    ('f3fe2d44-9a82-553f-88d8-1b0a9d7fde43', 'Harina', NULL, NULL, '7862a467-732d-5ac3-91f3-da043d96de5f'),
    ('53d5fbc2-4f8d-541a-a5db-75ee4f072909', 'Polvo para hornear', NULL, NULL, '7862a467-732d-5ac3-91f3-da043d96de5f'),
    ('1bff9561-0aaa-591d-a069-aeeee80182b9', 'Grasa vegetal', NULL, NULL, '7862a467-732d-5ac3-91f3-da043d96de5f'),
    ('3220577d-372b-59c8-a7c6-60fea9dc3e1a', 'Huevo', NULL, NULL, '6546c5e0-9df7-506a-b48d-ed9efcab88fb'),
    ('e55b58ed-f9bb-530f-881c-71c913cf3beb', 'Leche', NULL, NULL, '3426d21e-cac7-5df3-bfdc-a0918ccf5af6')
ON CONFLICT (id) DO NOTHING;

INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url) VALUES
    ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'Sopa de lentejas rojas con pollo y nabo', 'Sopa de lentejas rojas con pollo, nabo, verduras y caldo de verduras.', 55, 'Media', 8, 'spoonacular-715415', 'https://img.spoonacular.com/recipes/715415-556x370.jpg'),
    ('d443984a-eded-590d-8ba7-1c4216aaf5fb', 'Pasteles de carne de Natchitoches', 'Pasteles fritos de masa casera rellenos con carne vacuna, cerdo, morrón, cebolla de verdeo y condimentos.', 45, 'Media', 16, 'spoonacular-652968', 'https://img.spoonacular.com/recipes/652968-556x370.jpg')
ON CONFLICT (id) DO NOTHING;

INSERT INTO ingredientes_receta (id, receta_id, producto_id, nombre_ingrediente, cantidad, unidad) VALUES
    ('77deb818-963f-56e4-b41d-d702e2f64904', '06037ce2-4627-5196-a478-e4ecd537a9bc', '28e2ce43-fbbf-5a16-9489-2b6a1d12a6ed', 'Palta en cubos para decorar', NULL, NULL),
    ('a1253b0a-d2fa-5774-a735-63a4da18a00b', '06037ce2-4627-5196-a478-e4ecd537a9bc', '68e7b2e7-c0e0-51aa-8d5e-67e9e0a043c9', 'Microgreens para decorar', NULL, NULL),
    ('64868d1f-81c9-5cbd-b878-15acea5b5801', '06037ce2-4627-5196-a478-e4ecd537a9bc', '25117115-de6e-5e1a-aed9-e27843729f05', 'Albahaca picada para decorar', NULL, NULL),
    ('e77d1c60-a78a-5150-93b2-24cdd333ada1', '06037ce2-4627-5196-a478-e4ecd537a9bc', '6aad61d1-9c89-58c0-900b-90da37496211', 'Zanahorias medianas peladas y en cubos', 3, 'unidad'),
    ('0ccb4667-0e40-5b3e-925d-f4eca9649be9', '06037ce2-4627-5196-a478-e4ecd537a9bc', '3ffb2f7a-dc80-5733-8057-8bb13f0fd92e', 'Tallos de apio en cubos', 3, 'unidad'),
    ('58d81ea8-9609-5028-880e-cb8d65490fe0', '06037ce2-4627-5196-a478-e4ecd537a9bc', '9a4b8c06-e990-5ddd-a086-ae4fbe718c06', 'Pechuga de pollo cocida y desmenuzada', 280, 'g'),
    ('93889704-e15d-5ad4-b352-7d92692b4279', '06037ce2-4627-5196-a478-e4ecd537a9bc', '36a01c28-a785-59b1-ab05-efa910edec5d', 'Perejil italiano picado y extra para decorar', 30, 'g'),
    ('e98d9f8b-a6d4-5ebf-95a0-6177b2c5e9b4', '06037ce2-4627-5196-a478-e4ecd537a9bc', '7f16df6e-4613-59cb-b52d-059b6a63b362', 'Dientes de ajo bien picados', 6, 'unidad'),
    ('69bf0b6b-76ce-5085-b780-9500083869ee', '06037ce2-4627-5196-a478-e4ecd537a9bc', '20da9022-c83e-5808-afc8-9717132480f9', 'Aceite de oliva', 2, 'cda'),
    ('de918cc8-db28-5dec-9e73-41aa1b3dbcae', '06037ce2-4627-5196-a478-e4ecd537a9bc', 'aed0a77b-cccf-58e0-a526-55167ef1c457', 'Tomates perita en conserva escurridos, enjuagados y picados', 793.787, 'g'),
    ('e8bb37b0-94d2-52a6-981a-1b841905cf0f', '06037ce2-4627-5196-a478-e4ecd537a9bc', '893b00f6-a841-58f9-807b-23914fafc4b8', 'Lentejas rojas secas y enjuagadas', 360, 'g'),
    ('bb0d11db-6502-5068-9d0d-3b727443cde4', '06037ce2-4627-5196-a478-e4ecd537a9bc', 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe', 'Sal a gusto', NULL, NULL),
    ('819cc8d2-a372-532c-82b0-2154470d1c9c', '06037ce2-4627-5196-a478-e4ecd537a9bc', 'ed60b5b4-fbf0-5486-b2c8-0494132cc208', 'Pimienta negra a gusto', NULL, NULL),
    ('5fe707e7-0b66-5b5d-98fc-c3593c053cb6', '06037ce2-4627-5196-a478-e4ecd537a9bc', '0240b252-a457-52f4-94b1-87c2ddb26e49', 'Nabo grande pelado y en cubos', 1, 'unidad'),
    ('48bd5123-f35f-5c15-bba6-5be0b34cb839', '06037ce2-4627-5196-a478-e4ecd537a9bc', 'e57d80a2-2eac-5aca-8104-0b15e5dc3ae9', 'Caldo de verduras', 1.88, 'l'),
    ('70343e55-d5b0-59b3-b697-270df993a709', '06037ce2-4627-5196-a478-e4ecd537a9bc', '12ba1936-cbc5-5fe5-9330-f8a61e469be8', 'Cebolla amarilla mediana en cubos', 1, 'unidad'),
    ('37a2f278-4587-5779-a94e-52d33c9fa033', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '9954f472-3d12-52b5-a7e1-916f1d69936d', 'Aceite vegetal', 1, 'cda'),
    ('15d71b54-97ff-5ba2-8544-892ddc5091e4', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '9c9468de-a887-501b-aeca-025049c6faa5', 'Carne picada vacuna 80/20', 453.592, 'g'),
    ('a46aa9f0-ab87-5224-88d5-b5b22e815093', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '923ae982-eb45-578b-aa49-2ffac41725c4', 'Carne picada de cerdo', 453.592, 'g'),
    ('67e75cf8-7c90-54c8-9413-5d8c70e695c6', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '05923054-8fe5-5bc9-8118-d25713e9e15e', 'Cebolla de verdeo bien picada', 1, 'unidad'),
    ('0fc3882c-fd25-5155-9119-bf598b4f6d67', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '0bae6710-70fc-5693-9f77-7adefadd3aeb', 'Morrón chico bien picado', 1, 'unidad'),
    ('4ff58bec-fab2-5a00-b4bb-af1a6b88e0a3', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '7f16df6e-4613-59cb-b52d-059b6a63b362', 'Diente de ajo grande machacado', 1, 'unidad'),
    ('9d474937-01c5-5350-8adc-e311275f585b', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'de07f185-bc5d-5c15-9a16-bdce6e626553', 'Ají molido opcional', 0.25, 'cdta'),
    ('52c3ec33-0806-5a62-88c7-ae1273f9f867', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe', 'Sal a gusto para el relleno', NULL, NULL),
    ('7caaf715-a2d7-5424-911d-1a7b51c73c7a', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'ed60b5b4-fbf0-5486-b2c8-0494132cc208', 'Pimienta negra a gusto para el relleno', NULL, NULL),
    ('4da45d9f-feaa-5bd3-936c-71aa34042fb3', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'f3fe2d44-9a82-553f-88d8-1b0a9d7fde43', 'Harina común y extra para la superficie', 500, 'g'),
    ('7551d018-3ad9-53da-b3a1-1bc2078a1e2f', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe', 'Sal para la masa', 2, 'cdta'),
    ('e8384901-a740-5c80-82b9-3f699c97fe53', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '53d5fbc2-4f8d-541a-a5db-75ee4f072909', 'Polvo para hornear', 1, 'cdta'),
    ('07861dcd-f67d-5d04-bd4f-0d7b2d46e02d', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '1bff9561-0aaa-591d-a069-aeeee80182b9', 'Grasa vegetal', 102.5, 'g'),
    ('8ec8ad60-6355-5f27-89d2-7c60683bf886', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', '3220577d-372b-59c8-a7c6-60fea9dc3e1a', 'Huevos batidos', 2, 'unidad'),
    ('67c2b4aa-a17f-5cac-a67c-7c9e0218d3cf', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'e55b58ed-f9bb-530f-881c-71c913cf3beb', 'Leche', 244, 'ml')
ON CONFLICT (id) DO NOTHING;

INSERT INTO pasos_receta (id, receta_id, orden, descripcion) VALUES
    ('54207c74-b928-5ec7-a719-7234f1f9fd58', '06037ce2-4627-5196-a478-e4ecd537a9bc', 1, 'En una olla grande o cacerola profunda, calentá el aceite de oliva a fuego medio.'),
    ('e86fbc62-9317-5f13-b9f4-2f307615880e', '06037ce2-4627-5196-a478-e4ecd537a9bc', 2, 'Agregá la cebolla, las zanahorias y el apio. Cociná durante 8 a 10 minutos, revolviendo cada tanto, hasta que estén tiernos.'),
    ('aeccfd3e-5816-54a6-a0c2-e4f9490b199a', '06037ce2-4627-5196-a478-e4ecd537a9bc', 3, 'Agregá el ajo y cociná 2 minutos más, hasta que largue aroma. Condimentá con una pizca de sal y pimienta negra.'),
    ('7d9367b3-c60a-57a3-b219-5e469768f94e', '06037ce2-4627-5196-a478-e4ecd537a9bc', 4, 'Sumá los tomates, el nabo y las lentejas rojas. Mezclá bien.'),
    ('a09759b2-d70c-5570-8a1a-389b434b4d07', '06037ce2-4627-5196-a478-e4ecd537a9bc', 5, 'Incorporá el caldo de verduras, subí el fuego y llevá la sopa a hervor. Bajá a fuego suave y cociná durante 20 minutos, hasta que el nabo esté tierno y las lentejas estén cocidas.'),
    ('b3c1c7b6-c316-5411-84be-805b5bfa62ae', '06037ce2-4627-5196-a478-e4ecd537a9bc', 6, 'Agregá la pechuga de pollo y el perejil. Cociná 5 minutos más y ajustá los condimentos a gusto.'),
    ('002c401b-ce35-56f1-b65a-9444c87ea100', '06037ce2-4627-5196-a478-e4ecd537a9bc', 7, 'Serví la sopa caliente con perejil fresco y los toppings adicionales.'),
    ('b49ee2f8-21ac-552e-b5a7-1bf065b39613', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 1, 'En una sartén grande a fuego medio-alto, calentá el aceite vegetal y dorá la carne.'),
    ('522209dd-ba1d-513a-b6d8-c4df0c35b3c4', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 2, 'Agregá la cebolla de verdeo y el morrón. Cociná, revolviendo seguido, durante unos 5 minutos, hasta que las verduras estén blandas.'),
    ('7c767dcd-28fa-5a67-a764-53c33955d723', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 3, 'Agregá el ajo y salteá 1 minuto más.'),
    ('3e8c47f8-4bf7-5b84-b9e7-ab86d9761d76', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 4, 'Sumá el ají molido, sal y pimienta a gusto. Reservá el relleno hasta el momento de usar.'),
    ('ac0ea2e2-1d58-533a-8eae-b640b7aafd1f', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 5, 'Tamizá la harina, la sal y el polvo para hornear en un bowl grande o en el bowl de una procesadora.'),
    ('b4974746-c5eb-5ab1-ad40-e20c9a0a7b49', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 6, 'Procesá o integrá la grasa vegetal a mano hasta que la mezcla tenga textura de harina de maíz.'),
    ('a5913ae0-f9e9-5e77-bdca-ae18808a58f1', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 7, 'Mezclá los huevos con la leche y agregalos de a poco a la harina. Integrá bien hasta formar una masa blanda.'),
    ('0322b407-415b-5fb6-b7d4-3096d77b6356', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 8, 'Dividí la masa en 16 porciones iguales. Espolvoreá una superficie limpia y seca con harina.'),
    ('122cc623-f135-5c1f-b86c-a539f9ae4c6c', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 9, 'Formá un bollo con cada porción y estirá cada uno sobre la superficie enharinada hasta lograr un círculo de aproximadamente 15 a 20 cm de diámetro.'),
    ('74d703b6-168e-5a65-b729-5097feecc6ed', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 10, 'Colocá unas 2 cucharadas colmadas de relleno de carne sobre un lado de cada círculo, dejando un borde limpio.'),
    ('0e7c3caf-89e6-5fa7-8705-efb0b21bba7b', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 11, 'Pincelá los bordes con un poco de agua tibia. Doblá la masa sobre el relleno, uní los bordes y cerralos presionando con un tenedor. Repetí con el resto de la masa y el relleno.'),
    ('479f19fd-f5c5-57a6-964b-7b1f57bd544e', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 12, 'Calentá aceite para freír en una sartén mediana a fuego medio-alto.'),
    ('72b23a89-5770-53f8-8f3f-91bd0ae78b18', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 13, 'Cuando el aceite esté caliente, freí 1 o 2 pasteles por vez hasta que estén dorados de ambos lados, aproximadamente 3 minutos.'),
    ('413f9cb2-78a7-5841-bead-bfe897f0cd22', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 14, 'Retiralos del fuego y escurrilos sobre papel absorbente.'),
    ('67337bdd-8be7-51ea-a414-ee9b0355a442', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 15, 'Servilos calientes o a temperatura ambiente.')
ON CONFLICT (id) DO NOTHING;

INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas) VALUES
    ('624daafa-ece0-5709-bc11-c16387b63db2', '06037ce2-4627-5196-a478-e4ecd537a9bc', 477.24, 26.93, 51.78, 20.34),
    ('4fcb6901-3e88-5a07-8ee9-dcba477d3a96', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 355.01, 13.65, 25.07, 21.78)
ON CONFLICT (id) DO NOTHING;

INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido) VALUES
    ('191559e5-552f-5d2a-8b74-87d41dcd22ab', '06037ce2-4627-5196-a478-e4ecd537a9bc', 'Horno/Cocina'),
    ('336c75c9-4476-53ad-9472-a4565b2dec86', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'Horno/Cocina'),
    ('d2c05cde-5b05-5e45-a39a-395a8c49ff37', 'd443984a-eded-590d-8ba7-1c4216aaf5fb', 'Procesadora')
ON CONFLICT (id) DO NOTHING;
""");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.Sql("""
DELETE FROM receta_electrodomestico
WHERE receta_id IN ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'd443984a-eded-590d-8ba7-1c4216aaf5fb');

DELETE FROM info_nutricional_receta
WHERE receta_id IN ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'd443984a-eded-590d-8ba7-1c4216aaf5fb');

DELETE FROM pasos_receta
WHERE receta_id IN ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'd443984a-eded-590d-8ba7-1c4216aaf5fb');

DELETE FROM ingredientes_receta
WHERE receta_id IN ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'd443984a-eded-590d-8ba7-1c4216aaf5fb');

DELETE FROM recetas
WHERE id IN ('06037ce2-4627-5196-a478-e4ecd537a9bc', 'd443984a-eded-590d-8ba7-1c4216aaf5fb');

DELETE FROM productos
WHERE id IN ('28e2ce43-fbbf-5a16-9489-2b6a1d12a6ed', '68e7b2e7-c0e0-51aa-8d5e-67e9e0a043c9', '25117115-de6e-5e1a-aed9-e27843729f05', '6aad61d1-9c89-58c0-900b-90da37496211', '3ffb2f7a-dc80-5733-8057-8bb13f0fd92e', '9a4b8c06-e990-5ddd-a086-ae4fbe718c06', '36a01c28-a785-59b1-ab05-efa910edec5d', '7f16df6e-4613-59cb-b52d-059b6a63b362', '20da9022-c83e-5808-afc8-9717132480f9', 'aed0a77b-cccf-58e0-a526-55167ef1c457', '893b00f6-a841-58f9-807b-23914fafc4b8', 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe', 'ed60b5b4-fbf0-5486-b2c8-0494132cc208', '0240b252-a457-52f4-94b1-87c2ddb26e49', 'e57d80a2-2eac-5aca-8104-0b15e5dc3ae9', '12ba1936-cbc5-5fe5-9330-f8a61e469be8', '9954f472-3d12-52b5-a7e1-916f1d69936d', '9c9468de-a887-501b-aeca-025049c6faa5', '923ae982-eb45-578b-aa49-2ffac41725c4', '05923054-8fe5-5bc9-8118-d25713e9e15e', '0bae6710-70fc-5693-9f77-7adefadd3aeb', 'de07f185-bc5d-5c15-9a16-bdce6e626553', 'f3fe2d44-9a82-553f-88d8-1b0a9d7fde43', '53d5fbc2-4f8d-541a-a5db-75ee4f072909', '1bff9561-0aaa-591d-a069-aeeee80182b9', '3220577d-372b-59c8-a7c6-60fea9dc3e1a', 'e55b58ed-f9bb-530f-881c-71c913cf3beb')
  AND NOT EXISTS (
      SELECT 1
      FROM ingredientes_receta ir
      WHERE ir.producto_id = productos.id
  );

DELETE FROM categorias_producto
WHERE id IN ('aefcb56f-88d9-55b1-8b29-27855a28d151', '703d0951-e89f-5fc3-a09b-c0a9b3fd66ec', '6618c204-ff83-5ec5-87b7-a9abc04f2a81', 'db61671a-a9fa-50f6-bbff-8604d0773560', '709b31cd-0d7a-54d8-bd6c-d909f23e2603', '1de21387-806d-5b47-acb3-3a00fb36bf2f', '6546c5e0-9df7-506a-b48d-ed9efcab88fb', '3ef708eb-8f21-5107-95a1-28bfad9e4745', '3426d21e-cac7-5df3-bfdc-a0918ccf5af6', '7862a467-732d-5ac3-91f3-da043d96de5f', 'd42568b3-2b2a-59e7-960a-d70ddd29c39a')
  AND NOT EXISTS (
      SELECT 1
      FROM productos p
      WHERE p.categoria_producto_id = categorias_producto.id
  );
""");
        }
    }
   
        }
    
