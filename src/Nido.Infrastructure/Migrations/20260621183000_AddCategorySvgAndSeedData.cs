using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;
using System;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260621183000_AddCategorySvgAndSeedData")]
    public partial class AddCategorySvgAndSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icono_svg",
                table: "categorias_producto",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // 1. Set all existing category references to NULL temporarily to prevent FK issues
            migrationBuilder.Sql("UPDATE productos SET categoria_id = NULL;");
            migrationBuilder.Sql("UPDATE consumos_producto SET categoria_id = NULL;");

            // 2. Delete all existing categories to prevent unique index conflicts
            migrationBuilder.Sql("DELETE FROM categorias_producto;");

            // 3. Insert the 11 canonical categories
            migrationBuilder.Sql("""
                INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg)
                VALUES
                ('f0000000-0000-0000-0000-000000000001', 'Lácteos', 7, 'lacteos.svg'),
                ('f0000000-0000-0000-0000-000000000002', 'Carnes', 5, 'carnes.svg'),
                ('f0000000-0000-0000-0000-000000000003', 'Frutas', 14, 'frutas.svg'),
                ('f0000000-0000-0000-0000-000000000004', 'Verduras', 7, 'verduras.svg'),
                ('f0000000-0000-0000-0000-000000000005', 'Repostería', 180, 'reposteria.svg'),
                ('f0000000-0000-0000-0000-000000000006', 'Almacén', 365, 'almacen.svg'),
                ('f0000000-0000-0000-0000-000000000007', 'Bebidas', 30, 'bebidas.svg'),
                ('f0000000-0000-0000-0000-000000000008', 'Limpieza', 365, 'limpieza.svg'),
                ('f0000000-0000-0000-0000-000000000009', 'Baño', 365, 'bano.svg'),
                ('f0000000-0000-0000-0000-000000000010', 'Congelados', 90, 'congelados.svg'),
                ('f0000000-0000-0000-0000-000000000011', 'Otros', 30, 'otros.svg');
            """);

            // 4. Map products and consumos to their new category IDs based on name matching
            migrationBuilder.Sql("""
                UPDATE productos
                SET categoria_id = CASE
                    WHEN lower(nombre) LIKE '%leche%' OR lower(nombre) LIKE '%queso%' OR lower(nombre) LIKE '%yogur%' OR lower(nombre) LIKE '%crema%' OR lower(nombre) LIKE '%manteca%' OR lower(nombre) LIKE '%lacteo%' OR lower(nombre) LIKE '%ricota%' THEN 'f0000000-0000-0000-0000-000000000001'::uuid
                    WHEN lower(nombre) LIKE '%carne%' OR lower(nombre) LIKE '%pollo%' OR lower(nombre) LIKE '%cerdo%' OR lower(nombre) LIKE '%vaca%' OR lower(nombre) LIKE '%pescado%' OR lower(nombre) LIKE '%atun%' OR lower(nombre) LIKE '%merluza%' OR lower(nombre) LIKE '%bife%' OR lower(nombre) LIKE '%milanesa%' THEN 'f0000000-0000-0000-0000-000000000002'::uuid
                    WHEN lower(nombre) LIKE '%manzana%' OR lower(nombre) LIKE '%banana%' OR lower(nombre) LIKE '%naranja%' OR lower(nombre) LIKE '%limon%' OR lower(nombre) LIKE '%frutilla%' OR lower(nombre) LIKE '%uva%' OR lower(nombre) LIKE '%pera%' OR lower(nombre) LIKE '%durazno%' THEN 'f0000000-0000-0000-0000-000000000003'::uuid
                    WHEN lower(nombre) LIKE '%tomate%' OR lower(nombre) LIKE '%zanahoria%' OR lower(nombre) LIKE '%cebolla%' OR lower(nombre) LIKE '%lechuga%' OR lower(nombre) LIKE '%papa%' OR lower(nombre) LIKE '%batata%' OR lower(nombre) LIKE '%morron%' OR lower(nombre) LIKE '%ajo%' OR lower(nombre) LIKE '%espinaca%' THEN 'f0000000-0000-0000-0000-000000000004'::uuid
                    WHEN lower(nombre) LIKE '%harina%' OR lower(nombre) LIKE '%azucar%' OR lower(nombre) LIKE '%azúcar%' OR lower(nombre) LIKE '%chocolate%' OR lower(nombre) LIKE '%levadura%' OR lower(nombre) LIKE '%esencia%' OR lower(nombre) LIKE '%reposteria%' OR lower(nombre) LIKE '%dulce de leche%' THEN 'f0000000-0000-0000-0000-000000000005'::uuid
                    WHEN lower(nombre) LIKE '%arroz%' OR lower(nombre) LIKE '%fideo%' OR lower(nombre) LIKE '%pasta%' OR lower(nombre) LIKE '%aceite%' OR lower(nombre) LIKE '%sal%' OR lower(nombre) LIKE '%pimienta%' OR lower(nombre) LIKE '%oregano%' OR lower(nombre) LIKE '%condimento%' THEN 'f0000000-0000-0000-0000-000000000006'::uuid
                    WHEN lower(nombre) LIKE '%agua%' OR lower(nombre) LIKE '%jugo%' OR lower(nombre) LIKE '%gaseosa%' OR lower(nombre) LIKE '%soda%' OR lower(nombre) LIKE '%coca%' OR lower(nombre) LIKE '%cerveza%' OR lower(nombre) LIKE '%vino%' OR lower(nombre) LIKE '%bebida%' THEN 'f0000000-0000-0000-0000-000000000007'::uuid
                    WHEN lower(nombre) LIKE '%detergente%' OR lower(nombre) LIKE '%jabón%' OR lower(nombre) LIKE '%jabon%' OR lower(nombre) LIKE '%limpieza%' OR lower(nombre) LIKE '%lavandina%' OR lower(nombre) LIKE '%desinfectante%' THEN 'f0000000-0000-0000-0000-000000000008'::uuid
                    WHEN lower(nombre) LIKE '%shampoo%' OR lower(nombre) LIKE '%acondicionador%' OR lower(nombre) LIKE '%papel%' OR lower(nombre) LIKE '%dentifrico%' OR lower(nombre) LIKE '%desodorante%' OR lower(nombre) LIKE '%baño%' OR lower(nombre) LIKE '%bano%' THEN 'f0000000-0000-0000-0000-000000000009'::uuid
                    WHEN lower(nombre) LIKE '%congelado%' OR lower(nombre) LIKE '%hielo%' OR lower(nombre) LIKE '%helado%' THEN 'f0000000-0000-0000-0000-000000000010'::uuid
                    ELSE 'f0000000-0000-0000-0000-000000000011'::uuid
                END;
            """);

            migrationBuilder.Sql("""
                UPDATE consumos_producto
                SET categoria_id = CASE
                    WHEN lower(producto_nombre) LIKE '%leche%' OR lower(producto_nombre) LIKE '%queso%' OR lower(producto_nombre) LIKE '%yogur%' OR lower(producto_nombre) LIKE '%crema%' OR lower(producto_nombre) LIKE '%manteca%' OR lower(producto_nombre) LIKE '%lacteo%' OR lower(producto_nombre) LIKE '%ricota%' THEN 'f0000000-0000-0000-0000-000000000001'::uuid
                    WHEN lower(producto_nombre) LIKE '%carne%' OR lower(producto_nombre) LIKE '%pollo%' OR lower(producto_nombre) LIKE '%cerdo%' OR lower(producto_nombre) LIKE '%vaca%' OR lower(producto_nombre) LIKE '%pescado%' OR lower(producto_nombre) LIKE '%atun%' OR lower(producto_nombre) LIKE '%merluza%' OR lower(producto_nombre) LIKE '%bife%' OR lower(producto_nombre) LIKE '%milanesa%' THEN 'f0000000-0000-0000-0000-000000000002'::uuid
                    WHEN lower(producto_nombre) LIKE '%manzana%' OR lower(producto_nombre) LIKE '%banana%' OR lower(producto_nombre) LIKE '%naranja%' OR lower(producto_nombre) LIKE '%limon%' OR lower(producto_nombre) LIKE '%frutilla%' OR lower(producto_nombre) LIKE '%uva%' OR lower(producto_nombre) LIKE '%pera%' OR lower(producto_nombre) LIKE '%durazno%' THEN 'f0000000-0000-0000-0000-000000000003'::uuid
                    WHEN lower(producto_nombre) LIKE '%tomate%' OR lower(producto_nombre) LIKE '%zanahoria%' OR lower(producto_nombre) LIKE '%cebolla%' OR lower(producto_nombre) LIKE '%lechuga%' OR lower(producto_nombre) LIKE '%papa%' OR lower(producto_nombre) LIKE '%batata%' OR lower(producto_nombre) LIKE '%morron%' OR lower(producto_nombre) LIKE '%ajo%' OR lower(producto_nombre) LIKE '%espinaca%' THEN 'f0000000-0000-0000-0000-000000000004'::uuid
                    WHEN lower(producto_nombre) LIKE '%harina%' OR lower(producto_nombre) LIKE '%azucar%' OR lower(producto_nombre) LIKE '%azúcar%' OR lower(producto_nombre) LIKE '%chocolate%' OR lower(producto_nombre) LIKE '%levadura%' OR lower(producto_nombre) LIKE '%esencia%' OR lower(producto_nombre) LIKE '%reposteria%' OR lower(producto_nombre) LIKE '%dulce de leche%' THEN 'f0000000-0000-0000-0000-000000000005'::uuid
                    WHEN lower(producto_nombre) LIKE '%arroz%' OR lower(producto_nombre) LIKE '%fideo%' OR lower(producto_nombre) LIKE '%pasta%' OR lower(producto_nombre) LIKE '%aceite%' OR lower(producto_nombre) LIKE '%sal%' OR lower(producto_nombre) LIKE '%pimienta%' OR lower(producto_nombre) LIKE '%oregano%' OR lower(producto_nombre) LIKE '%condimento%' THEN 'f0000000-0000-0000-0000-000000000006'::uuid
                    WHEN lower(producto_nombre) LIKE '%agua%' OR lower(producto_nombre) LIKE '%jugo%' OR lower(producto_nombre) LIKE '%gaseosa%' OR lower(producto_nombre) LIKE '%soda%' OR lower(producto_nombre) LIKE '%coca%' OR lower(producto_nombre) LIKE '%cerveza%' OR lower(producto_nombre) LIKE '%vino%' OR lower(producto_nombre) LIKE '%bebida%' THEN 'f0000000-0000-0000-0000-000000000007'::uuid
                    WHEN lower(producto_nombre) LIKE '%detergente%' OR lower(producto_nombre) LIKE '%jabón%' OR lower(producto_nombre) LIKE '%jabon%' OR lower(producto_nombre) LIKE '%limpieza%' OR lower(producto_nombre) LIKE '%lavandina%' OR lower(producto_nombre) LIKE '%desinfectante%' THEN 'f0000000-0000-0000-0000-000000000008'::uuid
                    WHEN lower(producto_nombre) LIKE '%shampoo%' OR lower(producto_nombre) LIKE '%acondicionador%' OR lower(producto_nombre) LIKE '%papel%' OR lower(producto_nombre) LIKE '%dentifrico%' OR lower(producto_nombre) LIKE '%desodorante%' OR lower(producto_nombre) LIKE '%baño%' OR lower(producto_nombre) LIKE '%bano%' THEN 'f0000000-0000-0000-0000-000000000009'::uuid
                    WHEN lower(producto_nombre) LIKE '%congelado%' OR lower(producto_nombre) LIKE '%hielo%' OR lower(producto_nombre) LIKE '%helado%' THEN 'f0000000-0000-0000-0000-000000000010'::uuid
                    ELSE 'f0000000-0000-0000-0000-000000000011'::uuid
                END;
            """);

            // 5. Seed/ensure requested common products
            migrationBuilder.Sql("""
                INSERT INTO productos (id, nombre, codigo_barras, imagen_url, categoria_id, cantidad_compra_estandar, unidad_compra_estandar)
                VALUES
                ('e0000000-0000-0000-0000-000000000001', 'Chocolate', NULL, '/productos/chocolate.png', 'f0000000-0000-0000-0000-000000000005', 1.0, 'unidad'),
                ('e0000000-0000-0000-0000-000000000002', 'Harina', NULL, '/productos/harina.png', 'f0000000-0000-0000-0000-000000000005', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000003', 'Azúcar', NULL, '/productos/azucar.png', 'f0000000-0000-0000-0000-000000000005', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000004', 'Pollo', NULL, '/productos/pollo.png', 'f0000000-0000-0000-0000-000000000002', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000005', 'Carne picada', NULL, '/productos/carne_picada.png', 'f0000000-0000-0000-0000-000000000002', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000006', 'Tomate', NULL, '/productos/tomate.png', 'f0000000-0000-0000-0000-000000000004', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000007', 'Zanahoria', NULL, '/productos/zanahoria.png', 'f0000000-0000-0000-0000-000000000004', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000008', 'Manzana', NULL, '/productos/manzana.png', 'f0000000-0000-0000-0000-000000000003', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000009', 'Banana', NULL, '/productos/banana.png', 'f0000000-0000-0000-0000-000000000003', 1.0, 'kg'),
                ('e0000000-0000-0000-0000-000000000010', 'Jugo', NULL, '/productos/jugo.png', 'f0000000-0000-0000-0000-000000000007', 1.0, 'lt')
                ON CONFLICT (id) DO UPDATE SET
                    nombre = EXCLUDED.nombre,
                    categoria_id = EXCLUDED.categoria_id;
            """);

            // Also update standard database seed products' categories to point to the new ones
            migrationBuilder.Sql("""
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000001' WHERE id = '10000000-0000-0000-0000-000000000001'; -- Leche
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000001' WHERE id = '10000000-0000-0000-0000-000000000002'; -- Yogur
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000001' WHERE id = '10000000-0000-0000-0000-000000000003'; -- Queso
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000007' WHERE id = '10000000-0000-0000-0000-000000000004'; -- Agua
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000005'; -- Arroz
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000006'; -- Fideos
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000007'; -- Aceite de oliva
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000008'; -- Sal
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000002' WHERE id = '10000000-0000-0000-0000-000000000009'; -- Muslo de pollo
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000010'; -- Pimentón dulce
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000011'; -- Cebolla en polvo
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000012'; -- Orégano seco
                UPDATE productos SET categoria_id = 'f0000000-0000-0000-0000-000000000006' WHERE id = '10000000-0000-0000-0000-000000000013'; -- Ajo en polvo
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icono_svg",
                table: "categorias_producto");
        }
    }
}
