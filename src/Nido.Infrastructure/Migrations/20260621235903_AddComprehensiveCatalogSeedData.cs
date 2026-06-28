using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    public partial class AddComprehensiveCatalogSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icono",
                table: "categorias_producto",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE categorias_producto SET icono_svg = 'lacteos.svg', icono = 'milk' WHERE nombre = 'Lácteos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('d114512e-60a3-459d-891b-7b987c710b15', 'Lácteos', 14, 'lacteos.svg', 'milk') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0b08b9a8-553f-4b2c-83c4-70bbb7cf5a57', 'Leche Entera', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('48ad30ed-b7fa-4776-ad9a-ef6d105f3db3', 'Leche Descremada', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ff5e58fe-6761-43e1-9de1-5ec7feccbda8', 'Leche Deslactosada', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('91583316-a472-480c-aeed-9ba50e954e8a', 'Leche en Polvo', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2ac68e76-8105-456e-8aac-99a25f550886', 'Yogur Natural', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0e88211c-0c95-419c-8c8b-4d7f271fb1de', 'Yogur de Frutilla', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8da5f1f7-8507-4987-8684-7cdaf61be6af', 'Yogur de Vainilla', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('fa00f939-6cbd-4d8d-adb4-7ffbee1de387', 'Queso Cremoso', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f59d56b9-6411-44d2-aaf6-9b6013e029e1', 'Queso Muzzarella', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ce55e00d-01d9-45d1-8dc9-e9a9833c688e', 'Queso Rallado', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6c918c32-2eb6-4eae-917a-bf6492220ca4', 'Queso Port Salut', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f89155c8-4b20-4565-99b6-8db5eff12369', 'Queso Cheddar', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e76bb7be-8195-42c1-a3bd-af448dab969e', 'Queso Azul', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ce1881c9-2cd2-44d3-b189-ac6b1d4f8914', 'Ricota', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('3de0e6a7-f850-470b-981b-2ecab5540309', 'Crema de Leche', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('43a8d031-c103-4eaa-881c-f3a8a13821ea', 'Manteca', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a4445660-66de-4074-8728-dadaeb9d7859', 'Margarina', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d0e67137-1fa0-4fa0-addf-73821344a394', 'Dulce de Leche', (SELECT id FROM categorias_producto WHERE nombre = 'Lácteos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'carnes-vacunas.svg', icono = 'beef' WHERE nombre = 'Carnes Vacunas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('f21168f4-c142-49ab-a20a-b9d2de9ac198', 'Carnes Vacunas', 14, 'carnes-vacunas.svg', 'beef') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e7c1e806-c0b6-4aa6-a0d6-f6fc0afbc5d8', 'Carne Picada', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bd7db3ad-9e9b-4855-be1f-1778f0a8a6ed', 'Asado', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dbf6bb97-068a-4e31-97c3-86bfdcd87a03', 'Vacío', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('fc8011fe-9d16-4771-8b46-2756e30969d9', 'Matambre', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('77cd8048-6394-4052-a120-9cb99562ef24', 'Bife de Chorizo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('24a33818-bca9-40fb-8f13-c42a1a684040', 'Ojo de Bife', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c9d4c194-ddd6-4672-9579-6b01d5339005', 'Lomo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9df2ea61-8c08-48e5-acc8-a00fb435627b', 'Cuadril', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0af1049c-9642-42ec-8452-e0adeef0ad5f', 'Nalga', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1ac37471-58cd-4ac8-8514-b99d845e707e', 'Peceto', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9ca98755-9396-415a-8183-5316a34bf394', 'Roast Beef', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('32b42dd8-41d7-4111-ba80-29736d3e9978', 'Paleta', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9a667f9a-c11e-48a1-87e1-58a7dc7640e1', 'Entraña', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6be1e31a-8cf4-4d5c-bb28-666c8bc0f05f', 'Costillas', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Vacunas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'carnes-porcinas.svg', icono = 'beef' WHERE nombre = 'Carnes Porcinas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('fcd9ca73-3c03-40b8-848f-e322c86fc003', 'Carnes Porcinas', 14, 'carnes-porcinas.svg', 'beef') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2cba301c-a7f9-4f36-95f4-61dbcd93c050', 'Pechito de Cerdo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a8dc4889-29aa-4cd6-9438-392d1f50637b', 'Bondiola', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0a2ff5b6-5350-4a9e-8fe9-4e61270bbadb', 'Carré de Cerdo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('283c0e19-a600-4764-a935-2fd73834d762', 'Solomillo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e6fe6f86-3ae3-4642-88b5-be652330c95b', 'Matambre de Cerdo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('72d9a614-fbf4-42e3-b081-b42027b82819', 'Costillas de Cerdo', (SELECT id FROM categorias_producto WHERE nombre = 'Carnes Porcinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'pollo-aves.svg', icono = 'drumstick' WHERE nombre = 'Pollo y Aves';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('67c2103c-3446-4c97-93a5-0a1d01e88237', 'Pollo y Aves', 14, 'pollo-aves.svg', 'drumstick') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('53d7297b-901d-4318-8659-8bdd0bf03309', 'Pollo Entero', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('70623d51-ff8f-4e3d-a901-e02f07cdff89', 'Pechuga de Pollo', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2c781160-b406-465e-a661-f7191b0416e0', 'Pata Muslo', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7f1d8c0a-25ad-4355-9330-890d369df968', 'Alitas de Pollo', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('83d66c68-56aa-47e8-a64d-076774ca4788', 'Menudos de Pollo', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('be2b528a-6ef6-46a3-b60f-3d6aa5b76432', 'Pavo', (SELECT id FROM categorias_producto WHERE nombre = 'Pollo y Aves' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'pescados-mariscos.svg', icono = 'fish' WHERE nombre = 'Pescados y Mariscos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('fb7cb549-5e7e-4868-bc5d-fe4ba4d7002f', 'Pescados y Mariscos', 14, 'pescados-mariscos.svg', 'fish') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c3044d10-60f3-4b95-88e5-2e094b4a34a9', 'Merluza', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c2a70129-1f80-4435-b281-ff578410c84a', 'Salmón', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('aefbc3f3-4a33-44e2-9101-2ff385328bf1', 'Atún Fresco', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('43c2cd2e-4aca-45f0-8d70-a1406fcbd0e6', 'Pejerrey', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('df5ffaed-271c-431a-b13f-51f30079b311', 'Gatuzo', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b963b984-1163-418e-9012-2f0f0ff8d46f', 'Calamar', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('41cc07ae-0060-4e48-ae75-e4e334818f48', 'Langostinos', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('3134cfb7-6db7-4bee-96cc-b150413478c2', 'Mejillones', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('29993c1f-3134-46e8-a78b-a1a3c07c4266', 'Camarones', (SELECT id FROM categorias_producto WHERE nombre = 'Pescados y Mariscos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'frutas.svg', icono = 'apple' WHERE nombre = 'Frutas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('d7fc1dfd-7656-4e89-b201-7ef3961329b3', 'Frutas', 14, 'frutas.svg', 'apple') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2141e4c6-6f20-4a23-8b00-5cb3573686b7', 'Manzana Roja', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ed42f1a1-f357-43d2-a899-d54385687a58', 'Manzana Verde', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0b3e7876-82e6-4c48-b9ab-e642f1113758', 'Banana', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('aab0fd79-27db-4571-ba04-abf8ed05df99', 'Pera', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('16189342-04ad-46a3-842c-f752bcf9a418', 'Naranja', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9223a74b-eeb3-4bc4-8ade-1d6171b15752', 'Mandarina', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('990dd1e9-f518-4c77-8ca3-c2dd9b87abd4', 'Limón', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('312d86ee-c117-4978-8053-c6f101a66bda', 'Pomelo', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('54d3b073-3f43-48cf-9946-7212694e36c0', 'Durazno', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8629c44f-1f16-465d-8895-3588b5885d1a', 'Ciruela', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('836ef9f3-2a9f-47c8-b8df-a920546bcc0b', 'Frutilla', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bf64d2b1-705d-4dd3-a971-fb829926363c', 'Kiwi', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('73d56277-4bf0-4071-9afb-b2fa043a3b35', 'Mango', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('99d378c8-c21d-4b92-bfcf-f54377435eb0', 'Ananá', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('84a763f7-7597-43cc-bee8-e8f9c63b7875', 'Melón', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ab0ac9e2-baf6-4d3d-bc01-6bc33f4d3c91', 'Sandía', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ea6c9d77-a6aa-4aaa-83b2-ea774db19da2', 'Uva Blanca', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c9d5de69-7c41-4027-9f2d-c44d1390086e', 'Uva Negra', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('411f429c-61ab-461b-ae87-d2a4ff654e25', 'Cereza', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e4959696-0947-4ae5-8678-99d17a4f96a9', 'Arándanos', (SELECT id FROM categorias_producto WHERE nombre = 'Frutas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'verduras.svg', icono = 'carrot' WHERE nombre = 'Verduras';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('a9f859d8-e438-447f-88a9-61a4f77a4d47', 'Verduras', 14, 'verduras.svg', 'carrot') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('4ad0bf16-c817-4bf4-9a48-8be9fa3e3b39', 'Tomate Redondo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('933385bf-c951-4da6-96ef-cb6ad72ce8f3', 'Tomate Perita', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2968352a-971a-4dfe-b701-30a8b33a7d7e', 'Tomate Cherry', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a6645f34-1746-4194-a63b-2450f0be7d0e', 'Cebolla Blanca', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f845d01a-6cb7-4675-a197-34bc231ec660', 'Cebolla Morada', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d1136191-6f42-423c-a9e6-cb51a563c328', 'Cebolla de Verdeo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2f4d04aa-1b15-457b-851d-8c12907c7497', 'Papa Blanca', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e616dcd1-0ef3-43be-be50-277d1c0e0a5b', 'Papa Negra', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b398952f-066a-4bc8-8cf6-f25307966161', 'Batata', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('181ad109-7050-401e-ac0f-5741cfe0f55d', 'Zanahoria', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7db7fd24-ee54-453a-bc07-664ab09a6459', 'Zapallo Anco', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f09909fe-42fc-4b14-825f-c32fc682d202', 'Zapallo Cabutia', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('68632dd8-9c6a-4763-b749-bcfd488572cc', 'Zapallito Verde', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('98fdbb26-9b82-439a-8887-ab0022ac2bfc', 'Zucchini', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1dd84b24-ae24-410c-81a2-c54e0f2c5a34', 'Morrón Rojo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ddc9d882-e0c5-42a8-84f1-0aec5ecad371', 'Morrón Verde', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('aebf7a3c-4a65-47e4-8b65-d0709f8bda26', 'Morrón Amarillo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f2d37be7-1269-4c65-9116-fa31d2dadb64', 'Ajo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e1ca2c42-4ea4-4b10-99e4-cf925373e0d4', 'Lechuga Criolla', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c07ca7cd-973e-4442-a2b7-a20b569c701a', 'Lechuga Capuchina', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('455c30c0-1c41-402c-89f8-c5f79f27171e', 'Lechuga Mantecosa', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f66f1c2d-c89e-4f71-bd1f-810d931de1a3', 'Espinaca', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bcba96ea-9d21-4c9c-9285-70a314b1644e', 'Acelga', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a3af1fcd-d2ed-4d69-aa5f-c430ce11790c', 'Rúcula', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('09294c7c-3de7-42d1-a034-faa333999c16', 'Radicheta', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7fff5203-ab54-4bde-b30e-aca8b9452552', 'Apio', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a3d242fb-6544-4941-97a1-308e48c424c3', 'Puerro', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('fe307d8a-7513-4baf-910e-bae664769d59', 'Perejil', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f98c834f-a934-4c2f-8c53-7d6b2bd9615b', 'Cilantro', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1cda0431-545a-431f-8a13-e31140023216', 'Albahaca', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e1f3b133-ddb9-4886-ba26-8a9d4a92574e', 'Berenjena', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9ddab984-c1c8-47a9-a8a5-36e2d4717230', 'Pepino', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('07c792d1-2772-40ba-bef5-5780aa859cc7', 'Choclo', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('5cefba78-b0f6-49a1-a5c7-fa9d5354d5e3', 'Remolacha', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d33bc34b-4122-4012-9449-bcff2fd63431', 'Repollo Blanco', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7a6f013a-6244-4c0c-afc2-9063fb42c80d', 'Repollo Colorado', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('be0ef271-9d6d-445c-8243-3332478cdc9d', 'Brócoli', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a6c42e81-c9fc-4b86-996f-390dec152669', 'Coliflor', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('53d880a9-2a53-4296-9369-ca57d468ea43', 'Espárragos', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ea7170df-4d54-4142-a358-9a71f51931b7', 'Champiñones', (SELECT id FROM categorias_producto WHERE nombre = 'Verduras' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'legumbres.svg', icono = 'bean' WHERE nombre = 'Legumbres';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('63e3c35d-f03b-4255-817c-d2c9697f0e41', 'Legumbres', 14, 'legumbres.svg', 'bean') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a068f52e-46a9-436a-9afa-95864656c954', 'Lentejas', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b404ec29-a451-4080-987b-3f600ffaf90b', 'Garbanzos', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b0d078a3-8350-4cbe-ad6f-a7ee22ac9d17', 'Arvejas Secas', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('51dc30ed-67cd-4a70-84f2-9e014c15abfb', 'Porotos Blancos', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('34031947-1cd2-46c3-a41b-76bcd0bbe020', 'Porotos Negros', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('14241171-1744-4e40-8e61-d17a36a01fa8', 'Porotos Colorados', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('357a873f-c85e-48fc-a4f8-5f68f6d050ca', 'Porotos Pallares', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('db525071-9e81-4644-a124-1a2cd27af83f', 'Soja', (SELECT id FROM categorias_producto WHERE nombre = 'Legumbres' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'panificados.svg', icono = 'croissant' WHERE nombre = 'Panificados';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('b9b471f1-64b8-40f5-8c53-8a97e4189835', 'Panificados', 14, 'panificados.svg', 'croissant') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('907a8513-f3e0-465e-bfde-d7d7e2569b6e', 'Pan Francés', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('988d2c14-3574-4dda-b93f-eba01b18b8e9', 'Pan de Miga', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b61a00ab-8d8b-4a91-9c73-236c8c9b305a', 'Pan Lactal Blanco', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('93f256a8-3a94-4a7b-9ac7-e3523fa04baa', 'Pan Lactal Integral', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('372db2d1-ad66-44a2-ae4e-24b09c65c58d', 'Pan para Hamburguesa', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('40e93ef5-5362-4ef3-89a6-f30b96de19f8', 'Pan para Pancho', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('19fe547d-80b4-43da-9e75-162213913bb9', 'Galletas de Agua', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0308dc0e-566d-4adc-8c52-78dd6d409899', 'Galletas de Salvado', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('5a0e541a-6d81-44b2-b54c-49e4ee196152', 'Tostadas', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ffc60f81-90ee-4d76-b04d-0fbc51ab9504', 'Grisines', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('cb54ee2a-242d-4bc0-a692-ab5876f074fb', 'Bizcochitos de Grasa', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d539a049-c35d-4dbc-abf1-19b862871793', 'Medialunas', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('54b13fd7-34a2-4079-8780-ded383498c8e', 'Facturas', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d2ff85a5-a3d8-40f1-b2f2-34610802ee60', 'Prepizza', (SELECT id FROM categorias_producto WHERE nombre = 'Panificados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'pastas.svg', icono = 'utensils' WHERE nombre = 'Pastas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('a230fcf0-8df8-470a-99d9-77f9ef1bfc98', 'Pastas', 14, 'pastas.svg', 'utensils') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9ae3a325-9625-4a78-9258-c2a1e6e69369', 'Fideos Tallarines', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('333817c8-c6af-45b1-9edc-9819ea15dfe3', 'Fideos Mostacholes', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1d9f2176-8530-4c40-a3f6-9f83f370081c', 'Fideos Tirabuzón', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('31c1dd65-5f5b-4180-8163-04b40d008ccb', 'Fideos Moñito', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('604bfa02-3510-45cd-ac1b-c4f7b4445205', 'Fideos para Sopa', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('934731af-3897-4f00-851b-1450915d644a', 'Ñoquis', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1854649e-cc04-48f5-9e7a-d295947f0d56', 'Ravioles', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('04ef6717-eb23-47ee-a84d-717fe5a92cd1', 'Sorrentinos', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a9a69bf0-ac81-4095-988e-887dcdfaacda', 'Capeletis', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9a1ddf76-e2be-43db-aedb-6c4b106dfa8c', 'Lasaña', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f6e0938b-64b0-48da-b390-54a2d5c293dd', 'Canelones', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('20c91c56-b42a-4242-8217-89a2465e5b89', 'Tapas para Empanadas', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('3aee61ca-3516-4684-8852-00a7befb7a5b', 'Tapas para Pascualina', (SELECT id FROM categorias_producto WHERE nombre = 'Pastas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'arroz.svg', icono = 'wheat' WHERE nombre = 'Arroz';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('03f3d22b-e2fd-409b-bce3-da04174af39e', 'Arroz', 14, 'arroz.svg', 'wheat') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('41fa25de-3915-49b9-9996-d7faacd28593', 'Arroz Blanco', (SELECT id FROM categorias_producto WHERE nombre = 'Arroz' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('10c3a758-aa1c-48c1-8f66-b5f0bc88ada8', 'Arroz Integral', (SELECT id FROM categorias_producto WHERE nombre = 'Arroz' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a63909ae-507e-4a5c-9330-2aaf031b5020', 'Arroz Parboil', (SELECT id FROM categorias_producto WHERE nombre = 'Arroz' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('070fa890-2b29-49cc-ba2d-c03efc633045', 'Arroz Yamaní', (SELECT id FROM categorias_producto WHERE nombre = 'Arroz' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'cereales.svg', icono = 'wheat' WHERE nombre = 'Cereales';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('db72f9f4-0d62-4b91-a268-a225f6701c33', 'Cereales', 14, 'cereales.svg', 'wheat') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('be113f95-cd2f-4596-b21d-4557312e1bf0', 'Avena', (SELECT id FROM categorias_producto WHERE nombre = 'Cereales' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7610e9f6-8501-46aa-b373-a90c2d232a39', 'Granola', (SELECT id FROM categorias_producto WHERE nombre = 'Cereales' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('84c5fd80-37e1-4dac-a400-802b6a66f594', 'Copos de Maíz', (SELECT id FROM categorias_producto WHERE nombre = 'Cereales' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dbe3ce10-6d0a-4b05-9344-b1acd0383df1', 'Cereales de Chocolate', (SELECT id FROM categorias_producto WHERE nombre = 'Cereales' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'harinas.svg', icono = 'wheat' WHERE nombre = 'Harinas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('a376cc08-e972-4482-975e-695ef8c2fd17', 'Harinas', 14, 'harinas.svg', 'wheat') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b12823df-913a-4906-a9ff-c03ec68710d9', 'Harina de Trigo 000', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6f9090cd-0f1c-4e2f-9c67-d9711adba543', 'Harina de Trigo 0000', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ed9d1e43-a1a0-43c2-9666-f79c0a37971f', 'Harina Leudante', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bf3cf611-8816-4038-9a04-66f04a840d82', 'Harina Integral', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e26d253f-edab-4faf-8b73-9a4dec47707d', 'Harina de Maíz (Polenta)', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a0039d14-4d95-4aaa-abac-287eec421cc9', 'Harina de Almendras', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('14133765-190a-4150-a82f-5c9329c75e18', 'Harina de Avena', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('33656cfe-23a9-43ee-a3fc-c86dfad083f7', 'Almidón de Maíz (Maizena)', (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'reposteria.svg', icono = 'cake' WHERE nombre = 'Reposteria';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('0f892b28-3e05-451b-a3e8-bda46b4c6ed8', 'Reposteria', 14, 'reposteria.svg', 'cake') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('45698bb3-f8ae-4b5f-9823-f136f25d623d', 'Cacao en Polvo', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a5971ea3-7f23-4e5a-bcd7-2bbba13238ba', 'Chocolate Cobertura', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('50f1b496-187b-4683-be33-3e6918a7f4ad', 'Esencia de Vainilla', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a3ea8c2c-e92a-43ea-a741-68a5031ac6fb', 'Polvo de Hornear', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ffbe3cee-1ae9-4b58-8e57-7c1f83eac350', 'Levadura Fresca', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9ec1075e-e1cf-41db-8c3d-beb7c06617f5', 'Levadura Seca', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('40d9eae8-1922-4e9b-b7b6-5841da4c8dd4', 'Coco Rallado', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a8269f83-4b43-4fc6-a501-23cad4a93729', 'Gelatina sin Sabor', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('be307c64-9bb5-4ae2-89ff-171e071cb9a8', 'Gelatina de Frutilla', (SELECT id FROM categorias_producto WHERE nombre = 'Reposteria' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'azucar-endulzantes.svg', icono = 'sugar' WHERE nombre = 'Azúcar y Endulzantes';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('355af823-b043-455e-aadc-a94c24b3dd24', 'Azúcar y Endulzantes', 14, 'azucar-endulzantes.svg', 'sugar') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dad2945a-69c4-4118-b381-08aafb97ddba', 'Azúcar Blanca', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b6cc6d86-d713-4295-8a3b-a42f89bbe287', 'Azúcar Mascabo', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2d0f6542-016f-45a0-b1ae-2dbf36490a26', 'Azúcar Impalpable', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('caf23e8c-6d59-4096-89b7-be88ca620ed0', 'Edulcorante Líquido', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a420d6ad-67be-4741-bb26-f9b2c256ae28', 'Edulcorante en Sobres', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6b607347-1cf0-4593-855d-8d331c9a1f50', 'Stevia', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c94454a4-6f82-45bb-9e6b-77d1b9f88801', 'Miel', (SELECT id FROM categorias_producto WHERE nombre = 'Azúcar y Endulzantes' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'aceites.svg', icono = 'droplet' WHERE nombre = 'Aceites';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('c57debe6-9e91-4522-a195-30bf7a1ac1e8', 'Aceites', 14, 'aceites.svg', 'droplet') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ab41c99e-e518-4195-b0e6-6bcf603fd319', 'Aceite de Girasol', (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('39085b39-69d2-4fdf-b702-c6531d2a44ac', 'Aceite de Maíz', (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('38325ca0-34f4-4c2f-aafc-49d112cd0bf8', 'Aceite de Oliva', (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('95a66921-882c-4cf0-b5e8-7fc21eee52e2', 'Aceite de Coco', (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('940095f5-175a-4934-8cdf-db1c9c97d4b5', 'Rocío Vegetal', (SELECT id FROM categorias_producto WHERE nombre = 'Aceites' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'salsas-aderezos.svg', icono = 'bottle' WHERE nombre = 'Salsas y Aderezos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('33d023c6-fb78-4e67-99c1-7e936a5bb33c', 'Salsas y Aderezos', 14, 'salsas-aderezos.svg', 'bottle') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1a64dd26-00b6-4a2a-92f5-5cb6593f2688', 'Mayonesa', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('10d82c30-a67f-4b64-84d6-b10835a781ac', 'Ketchup', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('44a793ec-2c1e-456c-9597-f596c767c837', 'Mostaza', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('64b27382-f53b-47a6-b6b9-db814dd53c31', 'Salsa Golf', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2dec0eba-b50c-42eb-b33a-111074e03f40', 'Salsa de Soja', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('47d2389c-a8e7-4952-88fa-3600105fb4f5', 'Salsa Barbacoa', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b1e14b13-f4c0-4187-b196-e48fcb8f6530', 'Aceto Balsámico', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('36c2c558-023f-4d87-9c2c-3f64509c8495', 'Jugo de Limón', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d672beb0-b4c7-4ec2-b333-daec0b46d20e', 'Salsa de Tomate', (SELECT id FROM categorias_producto WHERE nombre = 'Salsas y Aderezos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'fiambres-embutidos.svg', icono = 'sausage' WHERE nombre = 'Fiambres y Embutidos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('4abcd016-b630-4bea-9f7d-0e2b12d0de2a', 'Fiambres y Embutidos', 14, 'fiambres-embutidos.svg', 'sausage') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('76806677-4005-4b5b-89b7-9160353da806', 'Jamón Cocido', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6198cc55-340e-4754-b9b8-9af2cc1a0b83', 'Jamón Crudo', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a2d70801-c5c6-440d-8a3f-76bacd9aa1b6', 'Paleta Cocida', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('467a9720-ccfe-4143-9d0e-5eaf1ef6395b', 'Salame', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f6062128-b9ec-482c-98a9-bf352dec87a0', 'Salamín', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('857d636c-de13-4c98-bbe8-11435d185138', 'Bondiola Curada', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('304ead4e-efbc-462d-82be-40aa6d157630', 'Panceta Salada', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('69360a53-ae26-4598-91dd-530fe5ea4a21', 'Panceta Ahumada', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('06b2d8bb-0d56-492c-bbbe-660852d37ed4', 'Chorizo Colorado', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0aeb0443-f24c-451b-b8e4-e8636b89c46b', 'Chorizo Fresco', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('55634b84-c436-4ab5-8f33-c4a2655bea0e', 'Salchichas', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('409320c7-7937-4ea7-b029-7d44a5b33d12', 'Morcilla', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8895ab89-33cf-4d37-bba2-3e2257ba3a2c', 'Mortadela', (SELECT id FROM categorias_producto WHERE nombre = 'Fiambres y Embutidos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'huevos.svg', icono = 'egg' WHERE nombre = 'Huevos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('aecee706-940d-492c-b860-1099eeeb7f01', 'Huevos', 14, 'huevos.svg', 'egg') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('95197678-16f2-4176-8892-b4c4854489ab', 'Huevos Blancos', (SELECT id FROM categorias_producto WHERE nombre = 'Huevos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9199d63c-a375-43e9-b260-c1d25ead27ce', 'Huevos Colorados', (SELECT id FROM categorias_producto WHERE nombre = 'Huevos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'congelados.svg', icono = 'snowflake' WHERE nombre = 'Congelados';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('67dc0ea2-fb30-4d84-8898-5ed10d0d0de8', 'Congelados', 14, 'congelados.svg', 'snowflake') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('21268309-e24a-4edc-a9ab-c107cdfeec58', 'Hamburguesas de Carne', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('effed9c1-f0ed-403d-ae41-6ad471918c63', 'Hamburguesas de Pollo', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('951d4ae9-6078-4ce7-8e76-0561500b02cc', 'Hamburguesas Vegetarias', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6ecc8f16-9f60-4b7b-8eb1-e0f6651bc4b7', 'Medallones de Merluza', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('91d3a2dd-8699-40ff-a7d6-a90038252641', 'Nuggets de Pollo', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f96ae0e4-24fb-47db-8a5c-378c686dda67', 'Papas Fritas Congeladas', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('39258aee-9682-44ef-80ed-892c314f87a7', 'Espinaca Congelada', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('616985c4-d9a8-4c10-a91a-e54d1152e8e6', 'Brócoli Congelado', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('d074f25a-5eb7-47b5-8815-c5bd2a054f66', 'Mix de Verduras Congelado', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a87a78eb-6241-45ea-9c55-9784826809ac', 'Frutos Rojos Congelados', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('72175246-58e2-454e-9ccb-17b96ad90846', 'Hielo', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1122fa0e-e707-4906-80db-dbad93ce3f5e', 'Helado', (SELECT id FROM categorias_producto WHERE nombre = 'Congelados' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'snacks.svg', icono = 'cookie' WHERE nombre = 'Snacks';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('a8aee174-6b4b-44de-b8ba-cee691d30073', 'Snacks', 14, 'snacks.svg', 'cookie') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('49adc8b6-4221-4392-9ad9-7789550c3d7d', 'Papas Fritas de Copetín', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('4d4db39d-fa68-424d-8d0a-2083afd2c49d', 'Palitos Salados', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2ab07b74-bd57-468c-9b53-2b33d7abe45c', 'Chizitos', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f6d70508-4806-4bf8-b789-bdd743bb702e', 'Maní Salado', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('5ce83a07-4b62-4634-bc50-b1776f316db2', 'Nachos', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8e9c0f48-1874-4eb9-98a4-8e5919fcb3c8', 'Pochoclo', (SELECT id FROM categorias_producto WHERE nombre = 'Snacks' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'golosinas.svg', icono = 'candy' WHERE nombre = 'Golosinas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('b59192a3-a003-46ae-afc2-6da03cc37415', 'Golosinas', 14, 'golosinas.svg', 'candy') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('79753b1a-2082-4fd5-ad04-65771844112b', 'Alfajores', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('63539903-3706-41bf-9029-326b433099fc', 'Chocolates', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6996704b-180e-4787-8cde-1d1c5acd96ad', 'Caramelos', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ac117bc1-9810-4db1-9ac6-1bc551dd3021', 'Chicles', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8a3c8f2c-dfdd-4a6d-9bb3-d92630e57902', 'Chupetines', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('499f4b8c-f38f-4d0d-a3f3-fdd858be4ebb', 'Galletitas Dulces', (SELECT id FROM categorias_producto WHERE nombre = 'Golosinas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'bebidas.svg', icono = 'glass-water' WHERE nombre = 'Bebidas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('40d743d0-0df4-4b54-a8b5-784e9e4acfdd', 'Bebidas', 14, 'bebidas.svg', 'glass-water') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e4672097-4035-43a4-ac72-f787e7392498', 'Agua Mineral sin Gas', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f0c9eaa7-e767-4bfe-8d80-8b5d9ed3ab2d', 'Agua Mineral con Gas', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('79a82eba-b9b0-45e1-969e-72b1bc2b95b6', 'Agua Saborizada', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('4c2ce455-d1ac-4792-a12e-6db13ed8846f', 'Gaseosa Cola', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a38984aa-e121-4d80-b831-75e7de975d3e', 'Gaseosa Limón', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('da106407-2d44-4cd4-b827-d86cdc540f14', 'Gaseosa Naranja', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0a0a0aab-7f7d-4de2-a862-b7fbaf831b62', 'Jugo en Polvo', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e9598739-0995-4a5f-946c-9625f33e7c7a', 'Jugo de Fruta', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0b0a275b-dbd1-4f23-aaf1-0f4ce873a2a7', 'Té Negro', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('aabcc203-f72f-4bc7-a74c-e529be2ae7f9', 'Té Verde', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('30ec43be-e6b9-4337-b429-89d80dfe366c', 'Mate Cocido', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0d9bc185-0726-44a7-8230-d346bdf440ba', 'Café Molido', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b21ef9f4-fb34-418f-8966-8a6660f5e814', 'Café Instantáneo', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ecfc5226-c08d-4204-97c8-877732285946', 'Café en Cápsulas', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0be8fa28-759f-460f-887d-2f441c3f8ee2', 'Yerba Mate', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'bebidas-alcoholicas.svg', icono = 'wine' WHERE nombre = 'Bebidas Alcohólicas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('d6882394-bec5-415f-a111-b7731fc1aa4e', 'Bebidas Alcohólicas', 14, 'bebidas-alcoholicas.svg', 'wine') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8622657c-2d5f-442f-b513-27cc7faa163b', 'Cerveza Rubia', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8ed0526f-78ff-4f3f-94b1-7c26c9f28db8', 'Cerveza Negra', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a841d49f-5f9b-48b2-b5a6-cf1ca61cba3b', 'Cerveza Roja', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dc68f255-a50f-4c6c-95f5-fbce3ab4479b', 'Vino Tinto', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8b8d3749-c6cf-439f-a147-c7ce3e9d78e6', 'Vino Blanco', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7e4a71df-1cb3-4c71-a530-180e70bb1ac3', 'Fernet', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('3d2c268c-2db4-48bf-a752-f7bde6c811a5', 'Vodka', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('4a467dfd-4254-470f-ad94-9fd0df3ddc9a', 'Ginebra', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('41b388b5-9d7a-4e3c-99f5-ca7701186066', 'Ron', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('29b11423-1e40-46d9-847f-625aa216f514', 'Whisky', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('356a8587-d772-444d-948d-744c65ce8a93', 'Espumante', (SELECT id FROM categorias_producto WHERE nombre = 'Bebidas Alcohólicas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'conservas.svg', icono = 'archive' WHERE nombre = 'Conservas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('8b829535-4d9e-4608-9adf-4c5459a51e9d', 'Conservas', 14, 'conservas.svg', 'archive') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2577071a-b47f-467b-8562-07be2a8af166', 'Atún en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2ee3dd11-f76b-491c-aebc-5154716b13d4', 'Caballa', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('18b5e403-cfbc-46ad-9c53-f987ce171a71', 'Sardinas', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('667e9aa2-1845-43cb-b30a-d3732b6892ce', 'Arvejas en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e4c78b75-3f4b-4bc3-9e4f-13638b284709', 'Choclo en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c9430444-5272-4a6c-9ef7-26abe7159ca1', 'Lentejas en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('80faaba1-a026-4b4f-af4e-015f68e41fb6', 'Puré de Tomate', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dc1f677f-ea90-497e-b89b-be2e6b6ec47a', 'Tomates Perita en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2a20cc72-898f-4c11-be50-426c0c7a069c', 'Pimientos Morrones en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2fc90472-0f10-4607-b99d-ee9408d624d5', 'Jardinera en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('3b8e255d-e58d-4f7a-a9b8-a8b196d186b0', 'Aceitunas Verdes', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('384e0491-bb0a-402a-917e-be7fcc8c3658', 'Aceitunas Negras', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('74251798-ba69-4543-8e74-6b5c3e400481', 'Palmitos', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('23eb9882-edf5-4f14-9428-f2487ab3be8e', 'Champiñones en Lata', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e467825b-09c2-4a23-b2a9-3a42edb2fcd4', 'Duraznos en Almíbar', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bce28a3e-7b96-4b7e-b7dc-e97659eccf33', 'Ananá en Almíbar', (SELECT id FROM categorias_producto WHERE nombre = 'Conservas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'condimentos.svg', icono = 'leaf' WHERE nombre = 'Condimentos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('3aa67aac-6057-4e14-93c2-98122c6a15b5', 'Condimentos', 14, 'condimentos.svg', 'leaf') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('35eb75e0-d1cc-4dc2-9df9-ae9d552200a0', 'Sal Fina', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('80e3b43d-cc92-4b27-aaec-fe86227c31ce', 'Sal Gruesa', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ee42a369-8cc2-4da9-8bb0-fbcc75dbf635', 'Pimienta Negra', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('feb09b86-d3c5-4bd3-82f6-391b4c6c9943', 'Pimienta Blanca', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e9bd38b9-71d9-499a-8428-4483f4a700f6', 'Orégano', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1063915f-dd52-400c-8478-291d9423f079', 'Ají Molido', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e3ad4a6b-b728-449e-9011-898f5a6d66ae', 'Pimentón', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8a3d9a41-85b3-40c4-a376-a0d10d8697b2', 'Comino', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0fe53b92-73e5-48a5-9ab1-e3d93acc7d37', 'Provenzal', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('23ee9dbc-1446-44bf-acb7-04ceee168e9d', 'Ajo en Polvo', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('83b869c4-e719-448e-92a8-e2af6defe1ca', 'Laurel', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bddf9eea-2ed8-493c-be20-42de3252a625', 'Albahaca Deshidratada', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b17b6cc1-20c2-44b1-8315-f61fbe42947e', 'Nuez Moscada', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('93d39e34-21aa-460e-9517-ac87a9d86891', 'Canela', (SELECT id FROM categorias_producto WHERE nombre = 'Condimentos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'productos-dieteticos.svg', icono = 'heart-pulse' WHERE nombre = 'Productos Dietéticos';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('b8e12b50-5e36-4e1d-a6f9-2839b59b210d', 'Productos Dietéticos', 14, 'productos-dieteticos.svg', 'heart-pulse') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('695030cf-c7bd-43ca-abf9-d1eeedf81d69', 'Mermelada Light', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Dietéticos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('58a31f3e-1609-4c15-902e-0c9eda3ff72f', 'Galletas de Arroz', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Dietéticos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('5e0f4290-35bd-48e3-a443-080234d315f9', 'Barritas de Cereal', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Dietéticos' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'productos-sin-tacc.svg', icono = 'wheat-off' WHERE nombre = 'Productos Sin TACC';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('6e4ae7cb-fdff-4be3-81cd-46e209fb7043', 'Productos Sin TACC', 14, 'productos-sin-tacc.svg', 'wheat-off') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c6168c11-73b6-4ed6-82cd-0311a426ef19', 'Premezcla sin TACC', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Sin TACC' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b20ae523-642d-493d-b69c-931dc12341b2', 'Fideos sin TACC', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Sin TACC' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bbc54503-ed64-4aa8-981a-8c3f8e0b4d07', 'Galletas sin TACC', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Sin TACC' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('51f92a5c-9d65-459e-91aa-d4110ceb9a93', 'Pan sin TACC', (SELECT id FROM categorias_producto WHERE nombre = 'Productos Sin TACC' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'limpieza.svg', icono = 'spray-can' WHERE nombre = 'Limpieza';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('4fc35727-114c-4be3-9998-ce023e0bfa21', 'Limpieza', 14, 'limpieza.svg', 'spray-can') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('5061abfc-001c-428d-bb5d-dcb07bd96ef8', 'Lavandina', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('35a36ab6-845c-4071-8bcf-b572b95bc5d0', 'Detergente', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f4efdcb9-619d-4522-94f1-892acd47e3df', 'Jabón en Polvo para Ropa', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b2478a2a-006c-4793-a94d-8cf3b1e71be4', 'Jabón Líquido para Ropa', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('65bc5923-94ee-46a6-bd85-08d3b420698a', 'Suavizante', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('86953b5a-dc69-4528-8ab2-61be89019b0b', 'Limpiador de Pisos', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('8be2b8cd-8022-418e-b894-a397bc31b324', 'Limpiavidrios', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0e51f411-53bc-472d-8462-1612239c18ce', 'Desengrasante', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('58dcd5bf-b9b5-495e-b180-b23e0286d6db', 'Desinfectante en Aerosol', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('b55bdaca-85ca-48b5-a45a-96093165da2f', 'Esponja', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('bd161b9c-2392-42f7-8d38-ffeaa78368ea', 'Virulana', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('844928ee-6759-423a-81fb-be6ce08782b4', 'Trapo de Piso', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('15462964-b310-41d1-a608-80d42a120bdd', 'Rejilla', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6437fb7f-8308-4687-8eb0-5d5b046c2265', 'Bolsas de Consorcio', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('1dae3bac-d36e-48f7-85d8-8cc1d6605a05', 'Bolsas de Residuos', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('acdff635-a0ec-4785-9bcd-ee75e7bb6187', 'Rollo de Cocina', (SELECT id FROM categorias_producto WHERE nombre = 'Limpieza' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'higiene-personal.svg', icono = 'bath' WHERE nombre = 'Higiene Personal';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('cc460110-cccc-489b-bc64-04137975f727', 'Higiene Personal', 14, 'higiene-personal.svg', 'bath') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('29ff48bc-42e1-4771-9536-15b23cad6d55', 'Jabón de Tocador', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('a6668210-6dfa-45f4-940e-f3e7e94f3079', 'Shampoo', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('537784ab-39ba-4de2-bb64-f47de9de8361', 'Acondicionador', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('25db7204-1c96-416d-9795-dc3842963bb1', 'Pasta Dental', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('97a46e7c-a600-4125-9fa0-a6e8d294c192', 'Cepillo de Dientes', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('25fc20d8-9ca6-463a-bc99-095d90c7b024', 'Desodorante Corporal', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0209af55-95de-45e4-b0bb-60aacbf58840', 'Papel Higiénico', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('f1d9a5b5-e9bc-4bc3-ab9e-d4aa8067a69e', 'Toallas Femeninas', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('e5b6a9b4-569b-4081-8554-a781aa084d43', 'Protectores Diarios', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('35cd682d-7ed8-4e61-8865-cca44222524c', 'Algodón', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('03bdc49f-29f9-463e-9227-00b75e385788', 'Hisopos', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2de8153a-cbff-4e21-b76e-bda52ea1bebf', 'Máquina de Afeitar', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('624ada32-d0e8-40c1-a9fe-2f7ee609ebdc', 'Espuma de Afeitar', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('33e57329-783b-405a-8519-dc9deab0306b', 'Crema Corporal', (SELECT id FROM categorias_producto WHERE nombre = 'Higiene Personal' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'mascotas.svg', icono = 'dog' WHERE nombre = 'Mascotas';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('b7c13f86-fdb3-4c42-ba33-d0f385ad5977', 'Mascotas', 14, 'mascotas.svg', 'dog') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('65dc1082-cbfb-43db-b911-9a1262965b27', 'Alimento para Perros', (SELECT id FROM categorias_producto WHERE nombre = 'Mascotas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ba487cea-a1f7-4642-b480-bb27c5c5c157', 'Alimento para Gatos', (SELECT id FROM categorias_producto WHERE nombre = 'Mascotas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('ec1bba79-f243-435c-b4d0-ec775960625f', 'Piedras Sanitarias', (SELECT id FROM categorias_producto WHERE nombre = 'Mascotas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('0b749c8d-5030-4328-acba-2d9264307207', 'Golosinas para Mascotas', (SELECT id FROM categorias_producto WHERE nombre = 'Mascotas' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'bebes.svg', icono = 'baby' WHERE nombre = 'Bebés';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('ad902278-235c-4770-b604-3b76548af1f4', 'Bebés', 14, 'bebes.svg', 'baby') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('c6653f97-3c5e-46cf-aa3f-25e17b24d6e0', 'Pañales', (SELECT id FROM categorias_producto WHERE nombre = 'Bebés' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('7cdb955d-57d3-49d8-809c-c44cd0dc510b', 'Óleo Calcáreo', (SELECT id FROM categorias_producto WHERE nombre = 'Bebés' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('dfc2c006-b5b5-46c7-900a-00d059a97ebe', 'Toallitas Húmedas', (SELECT id FROM categorias_producto WHERE nombre = 'Bebés' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('358c2b6a-3b0f-453f-8b6d-d730304250b7', 'Shampoo para Bebés', (SELECT id FROM categorias_producto WHERE nombre = 'Bebés' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2e996239-8210-4eff-a290-1f25145b0f03', 'Talco', (SELECT id FROM categorias_producto WHERE nombre = 'Bebés' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
UPDATE categorias_producto SET icono_svg = 'otros.svg', icono = 'package' WHERE nombre = 'Otros';
INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono) VALUES ('271d6a52-4876-44f0-815b-015dbef72ba2', 'Otros', 14, 'otros.svg', 'package') ON CONFLICT DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('6ec40af4-cad1-4d05-89ba-bcc29f9bb1e8', 'Pilas', (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('2ffc9dc0-3ae4-4b38-9413-c7d70fbd645e', 'Fósforos', (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('58cda8a1-e30f-449d-9ccf-feba6cedbd28', 'Velas', (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1)) ON CONFLICT (id) DO NOTHING;
INSERT INTO productos (id, nombre, categoria_id) VALUES ('9e8f3e82-0cef-4f29-98cc-a2c332f010c1', 'Insecticida', (SELECT id FROM categorias_producto WHERE nombre = 'Otros' LIMIT 1)) ON CONFLICT (id) DO NOTHING;

WITH compra_estandar AS (
    SELECT
        p.id,
        CASE
            WHEN c.nombre IN ('Bebidas', 'Bebidas Alcohólicas', 'Aceites') THEN 1.00::numeric
            WHEN c.nombre = 'Lácteos' AND (
                lower(p.nombre) LIKE '%leche%' OR
                lower(p.nombre) LIKE '%crema%'
            ) THEN 1.00::numeric
            WHEN c.nombre IN (
                'Carnes Vacunas',
                'Carnes Porcinas',
                'Pollo y Aves',
                'Pescados y Mariscos',
                'Frutas',
                'Verduras',
                'Legumbres',
                'Arroz',
                'Cereales',
                'Harinas',
                'Azúcar y Endulzantes'
            ) THEN 1.00::numeric
            WHEN c.nombre = 'Condimentos' AND (
                lower(p.nombre) LIKE '%sal%' OR
                lower(p.nombre) LIKE '%pimienta%' OR
                lower(p.nombre) LIKE '%orégano%' OR
                lower(p.nombre) LIKE '%oregano%' OR
                lower(p.nombre) LIKE '%ají%' OR
                lower(p.nombre) LIKE '%aji%' OR
                lower(p.nombre) LIKE '%pimentón%' OR
                lower(p.nombre) LIKE '%pimenton%' OR
                lower(p.nombre) LIKE '%comino%' OR
                lower(p.nombre) LIKE '%provenzal%' OR
                lower(p.nombre) LIKE '%laurel%' OR
                lower(p.nombre) LIKE '%albahaca%' OR
                lower(p.nombre) LIKE '%nuez moscada%' OR
                lower(p.nombre) LIKE '%canela%'
            ) THEN 1.00::numeric
            ELSE 1.00::numeric
        END AS cantidad,
        CASE
            WHEN c.nombre IN ('Bebidas', 'Bebidas Alcohólicas', 'Aceites') THEN 'lt'
            WHEN c.nombre = 'Lácteos' AND (
                lower(p.nombre) LIKE '%leche%' OR
                lower(p.nombre) LIKE '%crema%'
            ) THEN 'lt'
            WHEN c.nombre IN (
                'Carnes Vacunas',
                'Carnes Porcinas',
                'Pollo y Aves',
                'Pescados y Mariscos',
                'Frutas',
                'Verduras',
                'Legumbres',
                'Arroz',
                'Cereales',
                'Harinas',
                'Azúcar y Endulzantes'
            ) THEN 'kg'
            WHEN c.nombre = 'Condimentos' AND lower(p.nombre) LIKE '%sal%' THEN 'kg'
            ELSE 'unidad'
        END AS unidad
    FROM productos p
    LEFT JOIN categorias_producto c ON c.id = p.categoria_id
)
UPDATE productos p
SET cantidad_compra_estandar = compra_estandar.cantidad,
    unidad_compra_estandar = compra_estandar.unidad
FROM compra_estandar
WHERE p.id = compra_estandar.id
  AND (
      p.cantidad_compra_estandar IS NULL OR
      p.unidad_compra_estandar IS NULL
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icono",
                table: "categorias_producto");
        }
    }
}
