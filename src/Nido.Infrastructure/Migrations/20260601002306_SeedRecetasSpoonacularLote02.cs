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
    ('aefcb56f-88d9-55b1-8b29-27855a28d151', 'Aceites'),
    ('7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62', 'Arroces y pastas'),
    ('d35173a4-9bc1-570b-be2a-1efc9cc63d4c', 'Bebidas'),
    ('db61671a-a9fa-50f6-bbff-8604d0773560', 'Condimentos'),
    ('709b31cd-0d7a-54d8-bd6c-d909f23e2603', 'Conservas'),
    ('1de21387-806d-5b47-acb3-3a00fb36bf2f', 'Frutas'),
    ('90cffb0d-38b3-5f31-97e1-cc0fec80b7b6', 'Frutos secos'),
    ('6546c5e0-9df7-506a-b48d-ed9efcab88fb', 'Huevos'),
    ('3426d21e-cac7-5df3-bfdc-a0918ccf5af6', 'Lácteos'),
    ('7862a467-732d-5ac3-91f3-da043d96de5f', 'Repostería'),
    ('d42568b3-2b2a-59e7-960a-d70ddd29c39a', 'Verduras');

INSERT INTO recetas (id, nombre, descripcion, tiempo_coccion_min, dificultad, porciones, fuente_id, imagen_url) VALUES
    ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'Arroz con almendras y arvejas', 'Arroz especiado con arvejas, caldo de pollo y almendras tostadas.', 45, 'Media', 4, 'spoonacular-653399', 'https://img.spoonacular.com/recipes/653399-556x370.jpg'),
    ('f1837e2d-173f-56e5-99d7-a623332b0929', 'Arroz con zucchini', 'Arroz salteado con zucchini, cebolla morada, ajo y especias, cocido en caldo de pollo.', 45, 'Media', 4, 'spoonacular-665781', 'https://img.spoonacular.com/recipes/665781-556x370.jpg'),
    ('b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 'Arroz pakistaní', 'Arroz dorado con manteca, pasas de uva, arvejas y cebolla cocida.', 45, 'Media', 4, 'spoonacular-654361', 'https://img.spoonacular.com/recipes/654361-556x370.jpg'),
    ('ea02c8a0-c7e2-5655-b560-31c43800faa6', 'Arroz frito con mango', 'Arroz cocido en caldo de pollo con verduras, ají picante y mango en cubos.', 45, 'Media', 2, 'spoonacular-716311', 'https://img.spoonacular.com/recipes/716311-556x370.jpg'),
    ('7fa78ac4-50ef-560d-b240-336602872641', 'Arroz integral frito', 'Arroz integral salteado con huevo, verduras, aceite de sésamo y salsa de soja.', 25, 'Media', 2, 'spoonacular-643674', 'https://img.spoonacular.com/recipes/643674-556x370.jpg');

INSERT INTO ingredientes_receta (id, receta_id, producto_id, nombre_ingrediente, cantidad, unidad) VALUES
    ('5eecea08-df0d-51f9-ad95-3293b116daca', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Pimienta de Jamaica molida', 0.5, 'cdta'),
    ('d3550bad-4d56-5312-8935-9895ac94c37a', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Almendras crudas', 0.5, NULL),
    ('a3f6638e-12d7-54e2-9a70-a1183f767f11', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Manteca', 2, 'cda'),
    ('96d43406-3087-5d11-b0df-f46b370a46ee', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Caldo de pollo', 2, 'taza'),
    ('2ef7617f-9ba3-5282-a68a-0ae08b8bbd29', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Albahaca fresca para decorar', NULL, NULL),
    ('78e0f21f-6a1c-55aa-ae02-036328957247', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Pimienta molida fresca', 0.25, 'cdta'),
    ('cacd4380-22e6-5e04-b595-29ab27ddf8a9', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Pimentón para decorar', 0.5, 'cdta'),
    ('4828062e-b322-5df9-8b10-2e0c9cafdadb', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Arvejas', 2, 'taza'),
    ('4607cbcb-baa0-5c6a-ba06-57d78d8cd96c', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Arroz', 1, 'taza'),
    ('840690a5-aee9-533b-8582-de88973d2664', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', NULL, 'Sal', NULL, NULL),
    ('2419273a-091e-5395-a8e4-c6f266095322', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Arroz de grano largo', 1, 'taza'),
    ('8c3694b3-91dd-54f8-a9e3-e3a44c6a7ae1', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Dientes de ajo picados', 3, 'unidad'),
    ('0defffca-3c63-59de-83ca-f9c0b0da44a1', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Cebolla morada picada', 0.75, 'unidad'),
    ('f6f183f7-a440-5212-88d4-5b87acf93674', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Aceite de oliva', 1, 'cda'),
    ('a7e7f671-b5fc-5d79-8848-4c9f6bf0fe68', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Zucchini cortado en trozos chicos', 1, 'unidad'),
    ('e3e1f7c0-76be-598f-9dd0-cde119f8f3b4', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Caldo de pollo bajo en sodio', 2.5, 'taza'),
    ('55afd044-1a7d-5d3f-9b91-057efdcd04f5', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Pimentón', 1, 'cdta'),
    ('fd351de1-1cec-5093-b1f4-d9eb991d1b9d', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'Chile en polvo', 1, 'cdta'),
    ('81fad7cd-b7f1-5799-be06-5a8029563102', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Cebolla grande', 0.5, 'unidad'),
    ('9c25d564-ecd5-504d-b233-aecedf016ec5', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Manteca', 3, 'cda'),
    ('e5750cb9-4fe0-51e3-b6e4-7fbb6dee4c65', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Pasas de uva', 1, 'taza'),
    ('39d57142-6990-5449-9f86-7021fd6bd562', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Arvejas', 0.5, 'taza'),
    ('d30de7b5-fb57-594a-9792-e85073460e4b', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Arroz precocido', 2, 'taza'),
    ('49a875e3-3ed7-544c-9206-422cda662cc8', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Agua', 2, 'taza'),
    ('749e635a-c787-574a-8d1d-9437f10c3731', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', NULL, 'Sal', 1, 'cdta'),
    ('6593a7ba-be48-5244-b3ee-fa340f625fd1', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Caldo de pollo', 2, 'taza'),
    ('99c1b002-8a93-58dd-b3d8-084d5498bd48', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Cubos saborizadores', NULL, NULL),
    ('b5713c4c-b41d-59b5-8110-63148718a5e0', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Rodajas de mango en cubos', 3, 'unidad'),
    ('abc13b7e-35b2-5cfb-8f4a-fdf819bbfb0d', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Arroz', 1, 'taza'),
    ('43baa058-a433-54e3-b3ed-8ee5a2d7b9f7', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Verduras picadas', 1, 'taza'),
    ('d0dce4e5-9fbc-52e4-904a-bd6ac90ff38e', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', NULL, 'Ají picante Scotch Bonnet', 1, 'unidad'),
    ('1655ad64-0b93-5692-a3b6-bdc12f095332', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Azúcar rubia', 1, 'cda'),
    ('2b894638-7370-5dc1-9b96-1d58549523b4', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Zanahorias cortadas finas', 2, 'unidad'),
    ('f44501f7-1e0c-5da9-8eab-58be6022f84c', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Arroz integral cocido y frío', 2, 'taza'),
    ('36d5ce5e-5a1c-5ece-825e-5958c2930fb1', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Hongos crimini fileteados', 1, 'taza'),
    ('894a8abd-0712-5580-bf87-d40e38f1db41', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Huevos batidos', 2, 'unidad'),
    ('4b666846-1ccb-53d6-ae29-458614d50698', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Jengibre fresco picado', 2, 'cda'),
    ('ea377c94-7528-5384-aea1-fe548ff46aa8', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Ajo picado', 1, 'cdta'),
    ('18d8867d-6502-594c-ad52-8165deb57d88', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Chauchas picadas', 1, 'taza'),
    ('f0864efe-c7f5-5db6-b61e-31f1eaa43522', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Cebolla de verdeo picada', 0.5, 'taza'),
    ('5430e3dd-0706-5cd3-86d8-ae6565c6249c', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Aceite de sésamo tostado y un poco más para terminar', 1, 'cda'),
    ('710fef82-e8f0-5914-be67-06ecc82a1ed0', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Salsa de soja', 4, 'cda'),
    ('49c8829a-bd80-5cec-9689-e6999563f78c', '7fa78ac4-50ef-560d-b240-336602872641', NULL, 'Zucchini cortado fino', 1, 'unidad');

INSERT INTO pasos_receta (id, receta_id, orden, descripcion) VALUES
    ('b6c1bc50-4327-5afe-bdfe-b4fcc4b65919', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 1, 'Agregá la manteca y el arroz en una olla a fuego medio. Cociná durante unos 5 minutos.'),
    ('fec74272-5f8b-5ffd-858a-945d472bf7e3', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 2, 'Agregá las arvejas, la pimienta de Jamaica y la sal.'),
    ('94e24d0a-7e6c-5e65-ba37-3776eb5d66c9', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 3, 'Cociná durante unos 2 minutos.'),
    ('41650570-35e5-55f7-bf16-fe1ee7760116', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 4, 'Agregá el caldo de pollo o agua.'),
    ('c9b27051-6c2f-5f9d-9203-ce97cd452e97', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 5, 'Cociná tapado a fuego bajo durante 20 minutos.'),
    ('da069d9c-9e42-5503-9a76-da41af57e1b4', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 6, 'En una sartén chica, tostá las almendras con un poco de manteca y sal. Reservá y usá para decorar.'),
    ('938e4a9b-eedb-5969-b81f-4d95e2a6afe3', 'f1837e2d-173f-56e5-99d7-a623332b0929', 1, 'Colocá una olla a fuego medio-alto.'),
    ('3a236848-6162-5a22-b5f2-2742f38b06b3', 'f1837e2d-173f-56e5-99d7-a623332b0929', 2, 'Agregá el aceite de oliva, la cebolla morada y el ajo.'),
    ('abe58da9-a1ac-55c7-bfd7-77a52ccf5dab', 'f1837e2d-173f-56e5-99d7-a623332b0929', 3, 'Salteá hasta que la cebolla se ablande, aproximadamente 5 minutos.'),
    ('266874fd-28cd-5922-8ebf-4a24ea0ee30f', 'f1837e2d-173f-56e5-99d7-a623332b0929', 4, 'Agregá el zucchini y las especias.'),
    ('62c6c279-d2ea-5db7-be6f-aa67ba0ab128', 'f1837e2d-173f-56e5-99d7-a623332b0929', 5, 'Salteá hasta que el zucchini empiece a cocinarse, unos 5 minutos.'),
    ('8b5e5ba1-5bbe-594b-8429-cad80733f6f0', 'f1837e2d-173f-56e5-99d7-a623332b0929', 6, 'Agregá el caldo de pollo, mezclá y tapá. Bajá a fuego medio-bajo y cociná durante 20 minutos, hasta que el arroz esté tierno.'),
    ('16dbba48-4f9b-53f4-8e95-4642a79357d4', 'f1837e2d-173f-56e5-99d7-a623332b0929', 7, 'Separá los granos con un tenedor y serví.'),
    ('5b100fae-f8b1-5cc0-b52a-3c23311048b1', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 1, 'Colocá la cebolla en 2 tazas de agua y herví durante 10 a 20 minutos.'),
    ('e1bc8413-d8e1-51ed-bfea-3d8ce579e7a4', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 2, 'Retirá la cebolla del agua.'),
    ('cdf51834-66b1-579e-a69f-64649ec93d2b', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 3, 'Agregá las pasas de uva a la manteca y calentá.'),
    ('00fc5f85-76c3-588e-8c2e-0654b508fe95', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 4, 'Agregá el arroz a la manteca caliente con pasas. Mezclá constantemente hasta que el arroz se dore.'),
    ('713f36fc-7b8a-579f-b153-5c65eefb2f38', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 5, 'Agregá las arvejas. Si están congeladas, descongelalas antes.'),
    ('af82fabf-45ca-5783-9af4-5652c968d8ee', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 6, 'Agregá el agua sin la cebolla. Llevá a hervor, tapá y cociná a fuego bajo durante 5 minutos.'),
    ('e4094533-8e12-5496-9c03-872bf0cb426d', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 7, 'Si queda líquido, cociná destapado hasta que se evapore.'),
    ('78d6a7ce-0a5e-50e6-a05a-083c0807ae91', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 1, 'Lavá el arroz y hervilo a fuego medio con poca agua, porque después se termina de cocinar con caldo de pollo.'),
    ('9b3b5c89-3d13-5248-846c-3626ecccd7d9', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 2, 'Cuando el arroz esté apenas tierno y el agua inicial se haya evaporado, bajá el fuego, agregá el caldo de pollo y cociná hasta que se absorba por completo.'),
    ('b9333672-83ee-59b7-b89b-bdaa4d97b601', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 3, 'Subí el fuego y agregá las verduras picadas y el ají.'),
    ('10cf27b3-9392-5137-887b-04d561047c72', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 4, 'Agregá el cubo saborizador.'),
    ('2690066e-507f-5dcd-90dd-a40c7be4153c', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 5, 'Incorporá el mango en cubos y serví tibio con la proteína que prefieras.'),
    ('bf79afde-afdc-5935-82cb-4d4f5ec0cd2d', '7fa78ac4-50ef-560d-b240-336602872641', 1, 'Calentá una sartén grande a fuego medio y cubrila con aerosol antiadherente.'),
    ('6f4b44f2-bffb-5c7c-90cf-c229e90a2a1c', '7fa78ac4-50ef-560d-b240-336602872641', 2, 'Agregá los huevos batidos y revolvé casi constantemente con una espátula durante 3 minutos, hasta que queden revueltos, esponjosos y cocidos.'),
    ('e23b15a0-207a-5bb9-83f8-cb7ca0861148', '7fa78ac4-50ef-560d-b240-336602872641', 3, 'Retirá los huevos a un plato chico y limpiá la sartén.'),
    ('266c7cce-136e-5a68-be5d-6241a0812618', '7fa78ac4-50ef-560d-b240-336602872641', 4, 'Agregá 1 cucharada de aceite de sésamo a la sartén y subí el fuego a medio-alto.'),
    ('74f0bb6a-cd60-5e1e-8376-8130a03dd0a5', '7fa78ac4-50ef-560d-b240-336602872641', 5, 'Agregá las zanahorias, el zucchini, las chauchas, los hongos, el jengibre y el ajo.'),
    ('08d8bfa1-7bf4-5802-923d-4547d209c168', '7fa78ac4-50ef-560d-b240-336602872641', 6, 'Salteá, revolviendo seguido, durante 5 a 7 minutos, hasta que las verduras se ablanden un poco.'),
    ('7f3e711b-294b-5d97-a75d-65333d9935e7', '7fa78ac4-50ef-560d-b240-336602872641', 7, 'Agregá el arroz. La mezcla puede parecer seca, pero la idea es que el arroz se dore y quede crocante.'),
    ('148a2afa-b942-5eb3-b54b-16cc452d9777', '7fa78ac4-50ef-560d-b240-336602872641', 8, 'Salteá todo durante unos 2 minutos. En un bowl chico, mezclá la salsa de soja con el azúcar rubia hasta que se disuelva.'),
    ('6ffecf6a-c23b-5357-854c-c13ed0dbb29f', '7fa78ac4-50ef-560d-b240-336602872641', 9, 'Verté la salsa en la sartén y mezclá para cubrir todo de forma pareja.'),
    ('41a50e49-63ae-55ce-ba77-388311500332', '7fa78ac4-50ef-560d-b240-336602872641', 10, 'Terminá con un chorrito adicional de aceite de sésamo, aproximadamente 1 cucharadita.'),
    ('d6bb8830-4a81-5285-95e5-e618c556bd6e', '7fa78ac4-50ef-560d-b240-336602872641', 11, 'Agregá el huevo revuelto a la sartén y mezclá bien.'),
    ('90addf17-e4cd-539a-8bfe-3cdc86388f47', '7fa78ac4-50ef-560d-b240-336602872641', 12, 'Serví.');

INSERT INTO info_nutricional_receta (id, receta_id, calorias, proteinas, carbohidratos, grasas) VALUES
    ('8eae2d78-a49f-5769-a565-6e9de319fcc5', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 287.64, 8.19, 48.41, 6.65),
    ('62fb4f3d-e2de-5388-bcef-8dc037f4a697', 'f1837e2d-173f-56e5-99d7-a623332b0929', 251.09, 7.51, 44.75, 4.97),
    ('a758615b-c181-5e89-bc1f-7ac0d7f60259', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 542.39, 8.79, 106.77, 9.41),
    ('815ccd0d-290e-5dc4-9dd1-28daa8ca3cdf', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 486.24, 15.96, 95.36, 4.03),
    ('57040008-2d28-5f87-a53c-00adfb395bdb', '7fa78ac4-50ef-560d-b240-336602872641', 470.08, 18.2, 71.9, 13.56);

INSERT INTO receta_electrodomestico (id, receta_id, tipo_requerido) VALUES
    ('a0e7a774-ca07-5495-8ea9-e8d2fb2f4187', 'a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'Horno/Cocina'),
    ('a374fe43-0273-5f41-8bf9-8880ea50424e', 'f1837e2d-173f-56e5-99d7-a623332b0929', 'Horno/Cocina'),
    ('5a746f68-2683-5aaf-8452-6891fd0b64d5', 'b3d9dc7c-ae3b-5da4-8b1f-7facff41131e', 'Horno/Cocina'),
    ('fa8f3aa9-af1b-59f0-93c6-ccd8884e57b1', 'ea02c8a0-c7e2-5655-b560-31c43800faa6', 'Horno/Cocina'),
    ('0cd56c2f-f956-551d-9495-d697d4418685', '7fa78ac4-50ef-560d-b240-336602872641', 'Horno/Cocina');
""");
                 }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DELETE FROM receta_electrodomestico
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'ea02c8a0-c7e2-5655-b560-31c43800faa6', '7fa78ac4-50ef-560d-b240-336602872641');

DELETE FROM info_nutricional_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'ea02c8a0-c7e2-5655-b560-31c43800faa6', '7fa78ac4-50ef-560d-b240-336602872641');

DELETE FROM pasos_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'ea02c8a0-c7e2-5655-b560-31c43800faa6', '7fa78ac4-50ef-560d-b240-336602872641');

DELETE FROM ingredientes_receta
WHERE receta_id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'ea02c8a0-c7e2-5655-b560-31c43800faa6', '7fa78ac4-50ef-560d-b240-336602872641');

DELETE FROM recetas
WHERE id IN ('a5d4bcbf-6dc3-57de-a7d7-bbf5519c41d8', 'f1837e2d-173f-56e5-99d7-a623332b0929', NULL, 'ea02c8a0-c7e2-5655-b560-31c43800faa6', '7fa78ac4-50ef-560d-b240-336602872641');


DELETE FROM categorias_producto
WHERE id IN ('aefcb56f-88d9-55b1-8b29-27855a28d151', '7aa7c1a0-f8ce-5af1-bb4c-a3ce71a4ed62', 'd35173a4-9bc1-570b-be2a-1efc9cc63d4c', 'db61671a-a9fa-50f6-bbff-8604d0773560', '709b31cd-0d7a-54d8-bd6c-d909f23e2603', '1de21387-806d-5b47-acb3-3a00fb36bf2f', '90cffb0d-38b3-5f31-97e1-cc0fec80b7b6', '6546c5e0-9df7-506a-b48d-ed9efcab88fb', '3426d21e-cac7-5df3-bfdc-a0918ccf5af6', '7862a467-732d-5ac3-91f3-da043d96de5f', 'd42568b3-2b2a-59e7-960a-d70ddd29c39a');
""");
        }
    }

        
}
