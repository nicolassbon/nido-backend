using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRecetasSpoonacularLote02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                 {
            migrationBuilder.Sql("""
INSERT INTO categorias_producto (id, nombre) VALUES
    ('aefcb56f-88d9-55b1-8b29-27855a28d151'::uuid, 'Aceites'),
    ('7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62'::uuid, 'Arroces y pastas'),
    ('d35173a4-9bc1-570b-be2a-1efc9cc63d4c'::uuid, 'Bebidas'),
    ('db61671a-a9fa-50f6-bbff-8604d0773560'::uuid, 'Condimentos'),
    ('709b31cd-0d7a-54d8-bd6c-d909f23e2603'::uuid, 'Conservas'),
    ('1de21387-806d-5b47-acb3-3a00fb36bf2f'::uuid, 'Frutas'),
    ('90cffb0d-38b3-5f31-97e1-cc0fec80b7b6'::uuid, 'Frutos secos'),
    ('6546c5e0-9df7-506a-b48d-ed9efcab88fb'::uuid, 'Huevos'),
    ('3426d21e-cac7-5df3-bfdc-a0918ccf5af6'::uuid, 'Lácteos'),
    ('7862a467-732d-5ac3-91f3-da043d96de5f'::uuid, 'Repostería'),
    ('d42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid, 'Verduras');

INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id) VALUES
    ('ecc03f9f-1e61-5a0d-b488-d8e08ff018da'::uuid, 'Pimienta de Jamaica', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('c42f7b80-3815-525c-9f30-f143913ce663'::uuid, 'Almendras', NULL, NULL, '90cffb0d-38b3-5f31-97e1-cc0fec80b7b6'::uuid),
    ('d5d0d677-ebd5-5be8-927c-cf8e4057aa29'::uuid, 'Manteca', NULL, NULL, '3426d21e-cac7-5df3-bfdc-a0918ccf5af6'::uuid),
    ('db090066-dd5e-594f-a59a-508338e67b57'::uuid, 'Caldo de pollo', NULL, NULL, '709b31cd-0d7a-54d8-bd6c-d909f23e2603'::uuid),
    ('25117115-de6e-5e1a-aed9-e27843729f05'::uuid, 'Albahaca', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('ed60b5b4-fbf0-5486-b2c8-0494132cc208'::uuid, 'Pimienta', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('39b156da-6170-5aec-b4b3-d690684b4888'::uuid, 'Pimentón', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('68352ec0-2fd1-5ebd-897b-fb533add09d0'::uuid, 'Arvejas', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'Arroz', NULL, NULL, '7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62'::uuid),
    ('bd4d8594-e937-55d6-ab98-3f0b86c5f7fe'::uuid, 'Sal', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('7f16df6e-4613-59cb-b52d-059b6a63b362'::uuid, 'Ajo', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('12ba1936-cbc5-5fe5-9330-f8a61e469be8'::uuid, 'Cebolla', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('20da9022-c83e-5808-afc8-9717132480f9'::uuid, 'Aceite de oliva', NULL, NULL, 'aefcb56f-88d9-55b1-8b29-27855a28d151'::uuid),
    ('d47bc825-98f0-5224-9073-62f6588c4dcd'::uuid, 'Zucchini', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('d470cb71-0869-54b6-a984-b683a318b66f'::uuid, 'Chile en polvo', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('cfb56fc1-9d54-5477-bbb7-2cd371511037'::uuid, 'Pasas de uva', NULL, NULL, '1de21387-806d-5b47-acb3-3a00fb36bf2f'::uuid),
    ('25ee334e-8f40-5d73-b065-63da761cdc37'::uuid, 'Agua', NULL, NULL, 'd35173a4-9bc1-570b-be2a-1efc9cc63d4c'::uuid),
    ('1335c15d-4cef-5d97-bc5c-9da5c58b1029'::uuid, 'Cubo saborizador', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid),
    ('7821087c-327e-5b40-8724-b347e4bf152d'::uuid, 'Mango', NULL, NULL, '1de21387-806d-5b47-acb3-3a00fb36bf2f'::uuid),
    ('8d96564d-8408-5592-912a-3ddfabaa8214'::uuid, 'Verduras mixtas', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('8ebc8d3d-4689-55ba-83c2-0bae8a3ea0a8'::uuid, 'Ají picante Scotch Bonnet', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('415f4f25-bd66-528c-b718-3f6195f82473'::uuid, 'Azúcar rubia', NULL, NULL, '7862a467-732d-5ac3-91f3-da043d96de5f'::uuid),
    ('6aad61d1-9c89-58c0-900b-90da37496211'::uuid, 'Zanahoria', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('659ce871-9bb1-5de1-b455-192440979f71'::uuid, 'Arroz integral', NULL, NULL, '7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62'::uuid),
    ('f5f2b786-026b-5442-8ff4-4668e11feb0a'::uuid, 'Hongos crimini', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('3220577d-372b-59c8-a7c6-60fea9dc3e1a'::uuid, 'Huevo', NULL, NULL, '6546c5e0-9df7-506a-b48d-ed9efcab88fb'::uuid),
    ('e46228db-d390-59d6-a5b8-bb2995bddd0d'::uuid, 'Jengibre', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('df31ed9a-08ae-5076-81ae-456174fa3a44'::uuid, 'Chauchas', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('05923054-8fe5-5bc9-8118-d25713e9e15e'::uuid, 'Cebolla de verdeo', NULL, NULL, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid),
    ('91081d57-8e08-526a-980d-23aa64a08c99'::uuid, 'Aceite de sésamo', NULL, NULL, 'aefcb56f-88d9-55b1-8b29-27855a28d151'::uuid),
    ('93a4b755-67f9-5986-a502-bd49d0bd0f8e'::uuid, 'Salsa de soja', NULL, NULL, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid);

INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url) VALUES
    ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'Arroz con almendras y arvejas', 'Arroz especiado con arvejas, caldo de pollo y almendras tostadas.', 45, 'Media', 4, 'spoonacular-653399', 'https://img.spoonacular.com/recipes/653399-556x370.jpg'),
    ('f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'Arroz con zucchini', 'Arroz salteado con zucchini, cebolla morada, ajo y especias, cocido en caldo de pollo.', 45, 'Media', 4, 'spoonacular-665781', 'https://img.spoonacular.com/recipes/665781-556x370.jpg'),
    ('b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'Arroz pakistaní', 'Arroz dorado con manteca, pasas de uva, arvejas y cebolla cocida.', 45, 'Media', 4, 'spoonacular-654361', 'https://img.spoonacular.com/recipes/654361-556x370.jpg'),
    ('ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 'Arroz frito con mango', 'Arroz cocido en caldo de pollo con verduras, ají picante y mango en cubos.', 45, 'Media', 2, 'spoonacular-716311', 'https://img.spoonacular.com/recipes/716311-556x370.jpg'),
    ('7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'Arroz integral frito', 'Arroz integral salteado con huevo, verduras, aceite de sésamo y salsa de soja.', 25, 'Media', 2, 'spoonacular-643674', 'https://img.spoonacular.com/recipes/643674-556x370.jpg');

INSERT INTO ingredientes_receta (id, receta_id, producto_id, nombre_ingrediente, cantidad, unidad) VALUES
    ('5eecea08-df0d-51f9-ad95-3293b116daca'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'ecc03f9f-1e61-5a0d-b488-d8e08ff018da'::uuid, 'Pimienta de Jamaica molida', 0.5, 'cdta'),
    ('d3550bad-4d56-5312-8935-9895ac94c37a'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'c42f7b80-3815-525c-9f30-f143913ce663'::uuid, 'Almendras crudas', 0.5, NULL),
    ('a3f6638e-12d7-54e2-9a70-a1183f767f11'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'd5d0d677-ebd5-5be8-927c-cf8e4057aa29'::uuid, 'Manteca', 2, 'cda'),
    ('96d43406-3087-5d11-b0df-f46b370a46ee'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'db090066-dd5e-594f-a59a-508338e67b57'::uuid, 'Caldo de pollo', 2, 'taza'),
    ('2ef7617f-9ba3-5282-a68a-0ae08b8bbd29'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, '25117115-de6e-5e1a-aed9-e27843729f05'::uuid, 'Albahaca fresca para decorar', NULL, NULL),
    ('78e0f21f-6a1c-55aa-ae02-036328957247'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'ed60b5b4-fbf0-5486-b2c8-0494132cc208'::uuid, 'Pimienta molida fresca', 0.25, 'cdta'),
    ('cacd4380-22e6-5e04-b595-29ab27ddf8a9'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, '39b156da-6170-5aec-b4b3-d690684b4888'::uuid, 'Pimentón para decorar', 0.5, 'cdta'),
    ('4828062e-b322-5df9-8b10-2e0c9cafdadb'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, '68352ec0-2fd1-5ebd-897b-fb533add09d0'::uuid, 'Arvejas', 2, 'taza'),
    ('4607cbcb-baa0-5c6a-ba06-57d78d8cd96c'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'Arroz', 1, 'taza'),
    ('840690a5-aee9-533b-8582-de88973d2664'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe'::uuid, 'Sal', NULL, NULL),
    ('2419273a-091e-5395-a8e4-c6f266095322'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'Arroz de grano largo', 1, 'taza'),
    ('8c3694b3-91dd-54f8-a9e3-e3a44c6a7ae1'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, '7f16df6e-4613-59cb-b52d-059b6a63b362'::uuid, 'Dientes de ajo picados', 3, 'unidad'),
    ('0defffca-3c63-59de-83ca-f9c0b0da44a1'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, '12ba1936-cbc5-5fe5-9330-f8a61e469be8'::uuid, 'Cebolla morada picada', 0.75, 'unidad'),
    ('f6f183f7-a440-5212-88d4-5b87acf93674'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, '20da9022-c83e-5808-afc8-9717132480f9'::uuid, 'Aceite de oliva', 1, 'cda'),
    ('a7e7f671-b5fc-5d79-8848-4c9f6bf0fe68'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'd47bc825-98f0-5224-9073-62f6588c4dcd'::uuid, 'Zucchini cortado en trozos chicos', 1, 'unidad'),
    ('e3e1f7c0-76be-598f-9dd0-cde119f8f3b4'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'db090066-dd5e-594f-a59a-508338e67b57'::uuid, 'Caldo de pollo bajo en sodio', 2.5, 'taza'),
    ('55afd044-1a7d-5d3f-9b91-057efdcd04f5'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, '39b156da-6170-5aec-b4b3-d690684b4888'::uuid, 'Pimentón', 1, 'cdta'),
    ('fd351de1-1cec-5093-b1f4-d9eb991d1b9d'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'd470cb71-0869-54b6-a984-b683a318b66f'::uuid, 'Chile en polvo', 1, 'cdta'),
    ('81fad7cd-b7f1-5799-be06-5a8029563102'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, '12ba1936-cbc5-5fe5-9330-f8a61e469be8'::uuid, 'Cebolla grande', 0.5, 'unidad'),
    ('9c25d564-ecd5-504d-b233-aecedf016ec5'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'd5d0d677-ebd5-5be8-927c-cf8e4057aa29'::uuid, 'Manteca', 3, 'cda'),
    ('e5750cb9-4fe0-51e3-b6e4-7fbb6dee4c65'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'cfb56fc1-9d54-5477-bbb7-2cd371511037'::uuid, 'Pasas de uva', 1, 'taza'),
    ('39d57142-6990-5449-9f86-7021fd6bd562'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, '68352ec0-2fd1-5ebd-897b-fb533add09d0'::uuid, 'Arvejas', 0.5, 'taza'),
    ('d30de7b5-fb57-594a-9792-e85073460e4b'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'Arroz precocido', 2, 'taza'),
    ('49a875e3-3ed7-544c-9206-422cda662cc8'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, '25ee334e-8f40-5d73-b065-63da761cdc37'::uuid, 'Agua', 2, 'taza'),
    ('749e635a-c787-574a-8d1d-9437f10c3731'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe'::uuid, 'Sal', 1, 'cdta'),
    ('6593a7ba-be48-5244-b3ee-fa340f625fd1'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 'db090066-dd5e-594f-a59a-508338e67b57'::uuid, 'Caldo de pollo', 2, 'taza'),
    ('99c1b002-8a93-58dd-b3d8-084d5498bd48'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '1335c15d-4cef-5d97-bc5c-9da5c58b1029'::uuid, 'Cubos saborizadores', NULL, NULL),
    ('b5713c4c-b41d-59b5-8110-63148718a5e0'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7821087c-327e-5b40-8724-b347e4bf152d'::uuid, 'Rodajas de mango en cubos', 3, 'unidad'),
    ('abc13b7e-35b2-5cfb-8f4a-fdf819bbfb0d'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 'f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'Arroz', 1, 'taza'),
    ('43baa058-a433-54e3-b3ed-8ee5a2d7b9f7'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '8d96564d-8408-5592-912a-3ddfabaa8214'::uuid, 'Verduras picadas', 1, 'taza'),
    ('d0dce4e5-9fbc-52e4-904a-bd6ac90ff38e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '8ebc8d3d-4689-55ba-83c2-0bae8a3ea0a8'::uuid, 'Ají picante Scotch Bonnet', 1, 'unidad'),
    ('1655ad64-0b93-5692-a3b6-bdc12f095332'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '415f4f25-bd66-528c-b718-3f6195f82473'::uuid, 'Azúcar rubia', 1, 'cda'),
    ('2b894638-7370-5dc1-9b96-1d58549523b4'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '6aad61d1-9c89-58c0-900b-90da37496211'::uuid, 'Zanahorias cortadas finas', 2, 'unidad'),
    ('f44501f7-1e0c-5da9-8eab-58be6022f84c'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '659ce871-9bb1-5de1-b455-192440979f71'::uuid, 'Arroz integral cocido y frío', 2, 'taza'),
    ('36d5ce5e-5a1c-5ece-825e-5958c2930fb1'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'f5f2b786-026b-5442-8ff4-4668e11feb0a'::uuid, 'Hongos crimini fileteados', 1, 'taza'),
    ('894a8abd-0712-5580-bf87-d40e38f1db41'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '3220577d-372b-59c8-a7c6-60fea9dc3e1a'::uuid, 'Huevos batidos', 2, 'unidad'),
    ('4b666846-1ccb-53d6-ae29-458614d50698'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'e46228db-d390-59d6-a5b8-bb2995bddd0d'::uuid, 'Jengibre fresco picado', 2, 'cda'),
    ('ea377c94-7528-5384-aea1-fe548ff46aa8'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '7f16df6e-4613-59cb-b52d-059b6a63b362'::uuid, 'Ajo picado', 1, 'cdta'),
    ('18d8867d-6502-594c-ad52-8165deb57d88'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'df31ed9a-08ae-5076-81ae-456174fa3a44'::uuid, 'Chauchas picadas', 1, 'taza'),
    ('f0864efe-c7f5-5db6-b61e-31f1eaa43522'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '05923054-8fe5-5bc9-8118-d25713e9e15e'::uuid, 'Cebolla de verdeo picada', 0.5, 'taza'),
    ('5430e3dd-0706-5cd3-86d8-ae6565c6249c'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '91081d57-8e08-526a-980d-23aa64a08c99'::uuid, 'Aceite de sésamo tostado y un poco más para terminar', 1, 'cda'),
    ('710fef82-e8f0-5914-be67-06ecc82a1ed0'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, '93a4b755-67f9-5986-a502-bd49d0bd0f8e'::uuid, 'Salsa de soja', 4, 'cda'),
    ('49c8829a-bd80-5cec-9689-e6999563f78c'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'd47bc825-98f0-5224-9073-62f6588c4dcd'::uuid, 'Zucchini cortado fino', 1, 'unidad');

INSERT INTO pasos_receta (id, receta_id, orden, descripcion) VALUES
    ('b6c1bc50-4327-5afe-bdfe-b4fcc4b65919'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 1, 'Agregá la manteca y el arroz en una olla a fuego medio. Cociná durante unos 5 minutos.'),
    ('fec74272-5f8b-5ffd-858a-945d472bf7e3'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 2, 'Agregá las arvejas, la pimienta de Jamaica y la sal.'),
    ('94e24d0a-7e6c-5e65-ba37-3776eb5d66c9'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 3, 'Cociná durante unos 2 minutos.'),
    ('41650570-35e5-55f7-bf16-fe1ee7760116'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 4, 'Agregá el caldo de pollo o agua.'),
    ('c9b27051-6c2f-5f9d-9203-ce97cd452e97'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 5, 'Cociná tapado a fuego bajo durante 20 minutos.'),
    ('da069d9c-9e42-5503-9a76-da41af57e1b4'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 6, 'En una sartén chica, tostá las almendras con un poco de manteca y sal. Reservá y usá para decorar.'),
    ('938e4a9b-eedb-5969-b81f-4d95e2a6afe3'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 1, 'Colocá una olla a fuego medio-alto.'),
    ('3a236848-6162-5a22-b5f2-2742f38b06b3'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 2, 'Agregá el aceite de oliva, la cebolla morada y el ajo.'),
    ('abe58da9-a1ac-55c7-bfd7-77a52ccf5dab'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 3, 'Salteá hasta que la cebolla se ablande, aproximadamente 5 minutos.'),
    ('266874fd-28cd-5922-8ebf-4a24ea0ee30f'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 4, 'Agregá el zucchini y las especias.'),
    ('62c6c279-d2ea-5db7-be6f-aa67ba0ab128'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 5, 'Salteá hasta que el zucchini empiece a cocinarse, unos 5 minutos.'),
    ('8b5e5ba1-5bbe-594b-8429-cad80733f6f0'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 6, 'Agregá el caldo de pollo, mezclá y tapá. Bajá a fuego medio-bajo y cociná durante 20 minutos, hasta que el arroz esté tierno.'),
    ('16dbba48-4f9b-53f4-8e95-4642a79357d4'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 7, 'Separá los granos con un tenedor y serví.'),
    ('5b100fae-f8b1-5cc0-b52a-3c23311048b1'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 1, 'Colocá la cebolla en 2 tazas de agua y herví durante 10 a 20 minutos.'),
    ('e1bc8413-d8e1-51ed-bfea-3d8ce579e7a4'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 2, 'Retirá la cebolla del agua.'),
    ('cdf51834-66b1-579e-a69f-64649ec93d2b'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 3, 'Agregá las pasas de uva a la manteca y calentá.'),
    ('00fc5f85-76c3-588e-8c2e-0654b508fe95'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 4, 'Agregá el arroz a la manteca caliente con pasas. Mezclá constantemente hasta que el arroz se dore.'),
    ('713f36fc-7b8a-579f-b153-5c65eefb2f38'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 5, 'Agregá las arvejas. Si están congeladas, descongelalas antes.'),
    ('af82fabf-45ca-5783-9af4-5652c968d8ee'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 6, 'Agregá el agua sin la cebolla. Llevá a hervor, tapá y cociná a fuego bajo durante 5 minutos.'),
    ('e4094533-8e12-5496-9c03-872bf0cb426d'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 7, 'Si queda líquido, cociná destapado hasta que se evapore.'),
    ('78d6a7ce-0a5e-50e6-a05a-083c0807ae91'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 1, 'Lavá el arroz y hervilo a fuego medio con poca agua, porque después se termina de cocinar con caldo de pollo.'),
    ('9b3b5c89-3d13-5248-846c-3626ecccd7d9'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 2, 'Cuando el arroz esté apenas tierno y el agua inicial se haya evaporado, bajá el fuego, agregá el caldo de pollo y cociná hasta que se absorba por completo.'),
    ('b9333672-83ee-59b7-b89b-bdaa4d97b601'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 3, 'Subí el fuego y agregá las verduras picadas y el ají.'),
    ('10cf27b3-9392-5137-887b-04d561047c72'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 4, 'Agregá el cubo saborizador.'),
    ('2690066e-507f-5dcd-90dd-a40c7be4153c'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 5, 'Incorporá el mango en cubos y serví tibio con la proteína que prefieras.'),
    ('bf79afde-afdc-5935-82cb-4d4f5ec0cd2d'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 1, 'Calentá una sartén grande a fuego medio y cubrila con aerosol antiadherente.'),
    ('6f4b44f2-bffb-5c7c-90cf-c229e90a2a1c'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 2, 'Agregá los huevos batidos y revolvé casi constantemente con una espátula durante 3 minutos, hasta que queden revueltos, esponjosos y cocidos.'),
    ('e23b15a0-207a-5bb9-83f8-cb7ca0861148'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 3, 'Retirá los huevos a un plato chico y limpiá la sartén.'),
    ('266c7cce-136e-5a68-be5d-6241a0812618'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 4, 'Agregá 1 cucharada de aceite de sésamo a la sartén y subí el fuego a medio-alto.'),
    ('74f0bb6a-cd60-5e1e-8376-8130a03dd0a5'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 5, 'Agregá las zanahorias, el zucchini, las chauchas, los hongos, el jengibre y el ajo.'),
    ('08d8bfa1-7bf4-5802-923d-4547d209c168'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 6, 'Salteá, revolviendo seguido, durante 5 a 7 minutos, hasta que las verduras se ablanden un poco.'),
    ('7f3e711b-294b-5d97-a75d-65333d9935e7'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 7, 'Agregá el arroz. La mezcla puede parecer seca, pero la idea es que el arroz se dore y quede crocante.'),
    ('148a2afa-b942-5eb3-b54b-16cc452d9777'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 8, 'Salteá todo durante unos 2 minutos. En un bowl chico, mezclá la salsa de soja con el azúcar rubia hasta que se disuelva.'),
    ('6ffecf6a-c23b-5357-854c-c13ed0dbb29f'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 9, 'Verté la salsa en la sartén y mezclá para cubrir todo de forma pareja.'),
    ('41a50e49-63ae-55ce-ba77-388311500332'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 10, 'Terminá con un chorrito adicional de aceite de sésamo, aproximadamente 1 cucharadita.'),
    ('d6bb8830-4a81-5285-95e5-e618c556bd6e'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 11, 'Agregá el huevo revuelto a la sartén y mezclá bien.'),
    ('90addf17-e4cd-539a-8bfe-3cdc86388f47'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 12, 'Serví.');

INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas) VALUES
    ('8eae2d78-a49f-5769-a565-6e9de319fcc5'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 287.64, 8.19, 48.41, 6.65),
    ('62fb4f3d-e2de-5388-bcef-8dc037f4a697'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 251.09, 7.51, 44.75, 4.97),
    ('a758615b-c181-5e89-bc1f-7ac0d7f60259'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 542.39, 8.79, 106.77, 9.41),
    ('815ccd0d-290e-5dc4-9dd1-28daa8ca3cdf'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 486.24, 15.96, 95.36, 4.03),
    ('57040008-2d28-5f87-a53c-00adfb395bdb'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 470.08, 18.2, 71.9, 13.56);

INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido) VALUES
    ('a0e7a774-ca07-5495-8ea9-e8d2fb2f4187'::uuid, 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'Horno/Cocina'),
    ('a374fe43-0273-5f41-8bf9-8880ea50424e'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'Horno/Cocina'),
    ('5a746f68-2683-5aaf-8452-6891fd0b64d5'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'Horno/Cocina'),
    ('fa8f3aa9-af1b-59f0-93c6-ccd8884e57b1'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, 'Horno/Cocina'),
    ('0cd56c2f-f956-551d-9495-d697d4418685'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid, 'Horno/Cocina');
""");
                 }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DELETE FROM receta_electrodomestico
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid);

DELETE FROM info_nutricional_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid);

DELETE FROM pasos_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid);

DELETE FROM ingredientes_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid);

DELETE FROM recetas
WHERE id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8'::uuid, 'f1837e2d-173f-56e5-99d7-a623332b0929'::uuid, 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e'::uuid, 'ea02c8a0-c7e2-5655-b560-31c43800faa6'::uuid, '7fa78ac4-50ef-560d-b240-336602872641'::uuid);

DELETE FROM productos
WHERE id IN ('ecc03f9f-1e61-5a0d-b488-d8e08ff018da'::uuid, 'c42f7b80-3815-525c-9f30-f143913ce663'::uuid, 'd5d0d677-ebd5-5be8-927c-cf8e4057aa29'::uuid, 'db090066-dd5e-594f-a59a-508338e67b57'::uuid, '25117115-de6e-5e1a-aed9-e27843729f05'::uuid, 'ed60b5b4-fbf0-5486-b2c8-0494132cc208'::uuid, '39b156da-6170-5aec-b4b3-d690684b4888'::uuid, '68352ec0-2fd1-5ebd-897b-fb533add09d0'::uuid, 'f1cc9d34-717b-57a6-9799-eb9e889e0681'::uuid, 'bd4d8594-e937-55d6-ab98-3f0b86c5f7fe'::uuid, '7f16df6e-4613-59cb-b52d-059b6a63b362'::uuid, '12ba1936-cbc5-5fe5-9330-f8a61e469be8'::uuid, '20da9022-c83e-5808-afc8-9717132480f9'::uuid, 'd47bc825-98f0-5224-9073-62f6588c4dcd'::uuid, 'd470cb71-0869-54b6-a984-b683a318b66f'::uuid, 'cfb56fc1-9d54-5477-bbb7-2cd371511037'::uuid, '25ee334e-8f40-5d73-b065-63da761cdc37'::uuid, '1335c15d-4cef-5d97-bc5c-9da5c58b1029'::uuid, '7821087c-327e-5b40-8724-b347e4bf152d'::uuid, '8d96564d-8408-5592-912a-3ddfabaa8214'::uuid, '8ebc8d3d-4689-55ba-83c2-0bae8a3ea0a8'::uuid, '415f4f25-bd66-528c-b718-3f6195f82473'::uuid, '6aad61d1-9c89-58c0-900b-90da37496211'::uuid, '659ce871-9bb1-5de1-b455-192440979f71'::uuid, 'f5f2b786-026b-5442-8ff4-4668e11feb0a'::uuid, '3220577d-372b-59c8-a7c6-60fea9dc3e1a'::uuid, 'e46228db-d390-59d6-a5b8-bb2995bddd0d'::uuid, 'df31ed9a-08ae-5076-81ae-456174fa3a44'::uuid, '05923054-8fe5-5bc9-8118-d25713e9e15e'::uuid, '91081d57-8e08-526a-980d-23aa64a08c99'::uuid, '93a4b755-67f9-5986-a502-bd49d0bd0f8e'::uuid);

DELETE FROM categorias_producto
WHERE id IN ('aefcb56f-88d9-55b1-8b29-27855a28d151'::uuid, '7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62'::uuid, 'd35173a4-9bc1-570b-be2a-1efc9cc63d4c'::uuid, 'db61671a-a9fa-50f6-bbff-8604d0773560'::uuid, '709b31cd-0d7a-54d8-bd6c-d909f23e2603'::uuid, '1de21387-806d-5b47-acb3-3a00fb36bf2f'::uuid, '90cffb0d-38b3-5f31-97e1-cc0fec80b7b6'::uuid, '6546c5e0-9df7-506a-b48d-ed9efcab88fb'::uuid, '3426d21e-cac7-5df3-bfdc-a0918ccf5af6'::uuid, '7862a467-732d-5ac3-91f3-da043d96de5f'::uuid, 'd42568b3-2b2a-59e7-960a-d70ddd29c39a'::uuid);
""");
        }
    }

        
}
